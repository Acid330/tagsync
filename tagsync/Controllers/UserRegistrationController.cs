using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using tagsync.Models;

namespace tagsync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserRegistrationController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _supabaseUrl = "https://xavaoddkhecbwpgljrzu.supabase.co";
        private readonly string _serviceKey;

        public UserRegistrationController(IConfiguration configuration)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClient = new HttpClient(handler);

            _configuration = configuration;
            _serviceKey = _configuration["Supabase:ServiceRoleKey"];

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _serviceKey);
            _httpClient.DefaultRequestHeaders.Add("apikey", _serviceKey);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var payload = new { email = request.Email, password = request.Password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_supabaseUrl}/auth/v1/signup";
            var response = await _httpClient.PostAsync(url, content);
            var resultContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(new { message = "Registration failed", details = resultContent });

            return Ok(new { message = "User registered" });
        }

        [HttpPost("addUserData")]
        public async Task<IActionResult> AddUserData([FromBody] UserDataRequest request)
        {
            var getUserUrl = $"{_supabaseUrl}/auth/v1/admin/users?email={request.Email}";
            var userResponse = await _httpClient.GetAsync(getUserUrl);
            var userJson = await userResponse.Content.ReadAsStringAsync();

            if (!userResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "User lookup failed", details = userJson });

            using var doc = JsonDocument.Parse(userJson);

            if (!doc.RootElement.TryGetProperty("users", out var usersElement))
                return BadRequest(new { message = "Missing 'users' array in response" });

            if (usersElement.ValueKind != JsonValueKind.Array || usersElement.GetArrayLength() == 0)
                return NotFound(new { message = "User not found" });

            var user = usersElement[0];

            if (!user.TryGetProperty("id", out var idProp))
                return BadRequest(new { message = "User found but 'id' is missing", raw = user.GetRawText() });

            var uid = idProp.GetString();

            var insertUrl = $"{_supabaseUrl}/rest/v1/user_data";
            var insertPayload = new[]
            {
                new {
                    id = uid,
                    email = request.Email,
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    phone = request.Phone,
                    city = request.City,
                    address = request.Address
                }
            };

            var insertJson = JsonSerializer.Serialize(insertPayload);
            var insertContent = new StringContent(insertJson, Encoding.UTF8, "application/json");
            insertContent.Headers.Add("Prefer", "return=representation");

            var insertResponse = await _httpClient.PostAsync(insertUrl, insertContent);
            var insertResult = await insertResponse.Content.ReadAsStringAsync();

            if (!insertResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "Data insertion failed", details = insertResult });

            return Ok(new { message = "User data added" });
        }

        [HttpPost("changeUserData")]
        public async Task<IActionResult> ChangeUserData([FromBody] UserDataUpdateRequest request)
        {
            var getUrl = $"{_supabaseUrl}/rest/v1/user_data?email=eq.{request.Email}";
            var getResponse = await _httpClient.GetAsync(getUrl);
            var getContent = await getResponse.Content.ReadAsStringAsync();

            if (!getResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "User lookup failed", details = getContent });

            using var doc = JsonDocument.Parse(getContent);
            var users = doc.RootElement;

            if (users.ValueKind != JsonValueKind.Array || users.GetArrayLength() == 0)
                return NotFound(new { message = "User not found" });

            var user = users[0];
            if (!user.TryGetProperty("id", out var idProp))
                return BadRequest(new { message = "Missing user id", raw = user.GetRawText() });

            var uid = idProp.GetString();

            var updateDict = new Dictionary<string, object>();
            if (request.FirstName != null) updateDict["first_name"] = request.FirstName;
            if (request.LastName != null) updateDict["last_name"] = request.LastName;
            if (request.Phone != null) updateDict["phone"] = request.Phone;
            if (request.City != null) updateDict["city"] = request.City;
            if (request.Address != null) updateDict["address"] = request.Address;

            if (updateDict.Count == 0)
                return BadRequest(new { message = "No fields to update" });

            var updateJson = JsonSerializer.Serialize(updateDict);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            updateContent.Headers.Add("Prefer", "return=representation");

            var patchUrl = $"{_supabaseUrl}/rest/v1/user_data?id=eq.{uid}";
            var updateResponse = await _httpClient.PatchAsync(patchUrl, updateContent);
            var updateResult = await updateResponse.Content.ReadAsStringAsync();

            if (!updateResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "Update failed", details = updateResult });

            return Ok(new { message = "User data updated" });
        }


        [HttpPost("requestPasswordReset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email is required" });

            if (string.IsNullOrWhiteSpace(request.RedirectUrl))
                return BadRequest(new { message = "Redirect URL is required" });

            var redirectUrl = request.RedirectUrl;
            var url = $"{_supabaseUrl}/auth/v1/recover?redirect_to={Uri.EscapeDataString(redirectUrl)}";

            var payload = new { email = request.Email };
            var json = JsonSerializer.Serialize(payload);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
            httpRequest.Headers.Add("apikey", _serviceKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(new { message = "Password reset request failed", details = content });

            return Ok(new { message = "Password reset email sent" });
        }




        [HttpPost("testLogin")]
        public async Task<IActionResult> TestLogin([FromBody] ChangePasswordRequest request)
        {
            var loginPayload = new
            {
                email = request.Email,
                password = request.Password
            };

            var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

            var loginUrl = $"{_supabaseUrl}/auth/v1/token?grant_type=password";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _serviceKey);

            var response = await client.PostAsync(loginUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            return Ok(new
            {
                status = response.StatusCode,
                body = result
            });
        }


        [HttpGet("getUserData")]
        public async Task<IActionResult> GetUserData([FromQuery] string email)
        {
            var url = $"{_supabaseUrl}/rest/v1/user_data?email=eq.{email}&select=first_name,last_name,phone,city,address,email";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(new { message = "User lookup failed", details = content });

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return NotFound(new { message = "User not found" });

            var user = root[0];

            var result = new
            {
                Email = user.GetProperty("email").GetString(),
                FirstName = user.GetProperty("first_name").GetString(),
                LastName = user.GetProperty("last_name").GetString(),
                Phone = user.GetProperty("phone").GetString(),
                City = user.GetProperty("city").GetString(),
                Address = user.GetProperty("address").GetString()
            };

            return Ok(result);
        }


        [HttpGet("checkUserExists")]
        public async Task<IActionResult> CheckUserExists([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required" });

            var getUserUrl = $"{_supabaseUrl}/auth/v1/admin/users";
            var response = await _httpClient.GetAsync(getUserUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { message = "Supabase error", details = json });

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("users", out var usersElement))
                return BadRequest(new { message = "Missing 'users' array in response" });

            if (usersElement.ValueKind != JsonValueKind.Array)
                return BadRequest(new { message = "Invalid format for 'users' array" });

            foreach (var user in usersElement.EnumerateArray())
            {
                if (user.TryGetProperty("email", out var emailProp) && emailProp.GetString() == email)
                {
                    var uid = user.GetProperty("id").GetString();
                    return Ok(new { exists = true, uid });
                }
            }

            return Ok(new { exists = false });
        }

    }
}

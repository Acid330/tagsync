namespace tagsync.Models;

public class UserDataRequest
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public string City { get; set; }
    public string Address { get; set; }
}

public class UserDataUpdateRequest
{
    public string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
}

public class ChangePasswordRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class PasswordResetRequest
{
    public string Email { get; set; }
    public string? RedirectUrl { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

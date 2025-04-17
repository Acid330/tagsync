using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{

    [HttpGet("{userEmail}")]
    public async Task<IActionResult> GetWishlist(string userEmail)
    {
        var wishlist = await SupabaseConnector.Client
            .From<Wishlist>()
            .Where(x => x.UserEmail == userEmail)
            .Get();

        var productIds = wishlist.Models.Select(w => w.ProductId).ToHashSet();
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();

        var result = allProducts.Models
            .Where(p => productIds.Contains(p.Id))
            .Select(p =>
            {
                var characteristics = allParamsInt.Models
                    .Where(param => param.ProductId == p.Id)
                    .Select(param =>
                    {
                        var name = param.Name.ToLower();
                        var value = param.Value.ToString();
                        var translations = LocalizationHelper.ParameterTranslations.TryGetValue(name, out var tr) ? tr : null;

                        var dict = new Dictionary<string, object?>
                        {
                            { "name", param.Name },
                            { "value", value },
                            { "translations", translations }
                        };

                        if (LocalizationHelper.ValueSuffixes.TryGetValue(name, out var suffix))
                        {
                            dict["value_translations"] = new Dictionary<string, string>
                            {
                                { "uk", $"{value} {suffix.uk}" },
                                { "en", $"{value} {suffix.en}" }
                            };
                        }

                        return dict;
                    })
                    .Concat(
                        allParams.Models
                            .Where(param => param.ProductId == p.Id)
                            .Select(param =>
                            {
                                var name = param.Name.ToLower();
                                var translations = LocalizationHelper.ParameterTranslations.TryGetValue(name, out var tr) ? tr : null;

                                return new Dictionary<string, object?>
                                {
                                    { "name", param.Name },
                                    { "value", param.Value },
                                    { "translations", translations }
                                };
                            })
                    ).ToList();

                return new
                {
                    product_id = p.Id,
                    title = p.Title,
                    image_url = p.ImageUrl,
                    price = allParamsInt.Models.FirstOrDefault(x => x.ProductId == p.Id && x.Name == "price")?.Value,
                    characteristics
                };
            });

        return Ok(result);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto)
    {
        var item = new Wishlist
        {
            UserEmail = dto.UserEmail,
            ProductId = dto.ProductId,
            AddedAt = DateTime.UtcNow
        };

        try
        {
            await SupabaseConnector.Client.From<Wishlist>().Insert(item);
            return Ok(new { success = true });
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            if (ex.Message.Contains("foreign key constraint"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No user with this email was found."
                });
            }

            if (ex.Message.Contains("duplicate key"))
            {
                return Conflict(new
                {
                    success = false,
                    message = "The product is already in my favorites."
                });
            }

            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveFromWishlist([FromBody] AddToWishlistDto dto)
    {
        await SupabaseConnector.Client
            .From<Wishlist>()
            .Where(x => x.UserEmail == dto.UserEmail)
            .Where(x => x.ProductId == dto.ProductId)
            .Delete();

        return Ok(new { success = true });
    }

}

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

        var productIds = wishlist.Models.Select(w => w.product_id).ToHashSet();
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();


        var result = allProducts.Models
            .Where(p => productIds.Contains(p.Id))
            .Select(p =>
            {
                var characteristics = allParamsInt.Models
                    .Where(param => param.product_id == p.Id)
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
                            .Where(param => param.product_id == p.Id)
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

                var slug = p.Category?.ToLower();
                var translations_slug = LocalizationHelper.CategoryTranslations.TryGetValue(slug, out var trCat) ? trCat : null;

                var productRatings = allReviews.Models
                    .Where(r => r.product_id == p.Id)
                    .Select(r => r.average_rating)
                    .ToList();

                float? averageRating = productRatings.Count == 0
                ? null
                : (float)Math.Round(productRatings.Average(), 1);

                return new
                {
                    product_id = p.Id,
                    title = p.Title,
                    slug = p.Category?.ToLower(),
                    translations_slug,
                    images = productImages.Models
                        .Where(img => img.product_id == p.Id)
                        .Select(img => img.ImageUrl)
                        .ToList(),
                    average_rating = averageRating,
                    price = allParamsInt.Models.FirstOrDefault(x => x.product_id == p.Id && x.Name == "price")?.Value,
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
            product_id = dto.product_id,
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
            .Where(x => x.product_id == dto.product_id)
            .Delete();

        return Ok(new { success = true });
    }

}

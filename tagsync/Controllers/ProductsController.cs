using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string category, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();


        var filteredProducts = allProducts.Models
            .Where(p => p.Category?.ToLower() == category.ToLower())
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        var result = filteredProducts.Select(p =>
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

            var productRatings = allReviews.Models
                .Where(r => r.ProductId == p.Id)
                .Select(r => r.Rating)
                .ToList();

            float? averageRating = productRatings.Count == 0
                ? null
                : (float)Math.Round(productRatings.Average(), 1)
;

            return new
            {
                product_id = p.Id,
                title = p.Title,
                images = productImages.Models
                    .Where(img => img.ProductId == p.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                price = allParamsInt.Models.FirstOrDefault(x => x.ProductId == p.Id && x.Name == "price")?.Value,
                average_rating = averageRating,
                characteristics
            };
        });

        return Ok(result);
    }


    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters([FromQuery] string category)
    {
        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var productIds = allProducts.Models
            .Where(p => p.Category?.ToLower() == category.ToLower())
            .Select(p => p.Id)
            .ToHashSet();

        var stringFilters = allParams.Models
            .Where(p => productIds.Contains(p.ProductId))
            .GroupBy(p => p.Name.ToLower())
            .Select(g => new
            {
                name = g.Key,
                values = g.Select(x => x.Value).Distinct().ToList(),
                translations = LocalizationHelper.ParameterTranslations.TryGetValue(g.Key, out var tr) ? tr : null
            });

        var intFilters = allParamsInt.Models
            .Where(p => productIds.Contains(p.ProductId))
            .GroupBy(p => p.Name.ToLower())
            .Select(g =>
            {
                var min = g.Min(x => x.Value);
                var max = g.Max(x => x.Value);
                var name = g.Key;
                var translations = LocalizationHelper.ParameterTranslations.TryGetValue(name, out var tr) ? tr : null;

                Dictionary<string, string>? valueTranslations = null;
                if (LocalizationHelper.ValueSuffixes.TryGetValue(name, out var suffix))
                {
                    valueTranslations = new Dictionary<string, string>
                    {
                        { "uk", $"{min}-{max} {suffix.uk}" },
                        { "en", $"{min}-{max} {suffix.en}" }
                    };
                }

                return new
                {
                    name,
                    min,
                    max,
                    translations,
                    value_translations = valueTranslations
                };
            });

        return Ok(new
        {
            string_filters = stringFilters,
            int_filters = intFilters
        });
    }
}

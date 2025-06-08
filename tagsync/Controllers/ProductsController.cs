using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] int? product_id, [FromQuery] int? page, [FromQuery] int? limit)
    {
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();

        var filteredProducts = allProducts.Models
            .Where(p =>
                (string.IsNullOrWhiteSpace(category) || p.Category?.ToLower() == category.ToLower()) &&
                (!product_id.HasValue || p.Id == product_id.Value))
            .ToList();

        int totalCount = filteredProducts.Count;
        if (page.HasValue && limit.HasValue)
        {
            int p = Math.Max(page.Value, 1);
            int l = Math.Max(limit.Value, 1);
            filteredProducts = filteredProducts.Skip((p - 1) * l).Take(l).ToList();
        }

        var result = filteredProducts.Select(p =>
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

            var productRatings = allReviews.Models
                .Where(r => r.product_id == p.Id)
                .Select(r => r.rating)
                .ToList();

            float? averageRating = productRatings.Count == 0
                ? null
                : (float)Math.Round(productRatings.Average(), 1);

            return new
            {
                product_id = p.Id,
                title = p.Title,
                slug = p.Category?.ToLower(),
                translations_slug = LocalizationHelper.CategoryTranslations.TryGetValue(p.Category?.ToLower() ?? "", out var slugTr) ? slugTr : null,
                images = productImages.Models
                    .Where(img => img.product_id == p.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                price = allParamsInt.Models.FirstOrDefault(x => x.product_id == p.Id && x.Name == "price")?.Value,
                rating = averageRating,
                characteristics
            };
        });

        return Ok(new
        {
            count_category = allProducts.Models.Count(p => p.Category?.ToLower() == category?.ToLower()),
            count_page = filteredProducts.Count,
            products = result
        });

    }




    [HttpGet("category")]
    public async Task<IActionResult> GetCategories()
    {
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();

        var categoryImages = new Dictionary<string, string>
        {
            { "cpu", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/cpu.svg" },
            { "storage", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/storage.svg" },
            { "gpu", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/gpu.svg" },
            { "motherboard", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/motherboard.svg" },
            { "ram", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/ram.svg" },
            { "cooler", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/cooler.svg" },
            { "case", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/case.svg" },
            { "psu", "https://xavaoddkhecbwpgljrzu.supabase.co/storage/v1/object/public/images/category/power-supply.svg" },
        };

        var categories = allProducts.Models
            .GroupBy(p => p.Category?.ToLower())
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g =>
            {
                var slug = g.Key!;
                var count = g.Count();

                var translations = LocalizationHelper.CategoryTranslations.TryGetValue(slug, out var tr) ? tr : null;
                var image = categoryImages.TryGetValue(slug, out var img) ? img : null;

                return new
                {
                    slug,
                    img = image,
                    count,
                    translations_slug = translations

                };
            })
            .ToList();

        return Ok(categories);
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
            .Where(p => productIds.Contains(p.product_id))
            .GroupBy(p => p.Name.ToLower())
            .Select(g =>
            {
                var name = g.Key;
                var translations = LocalizationHelper.ParameterTranslations.TryGetValue(name, out var tr) ? tr : null;
                var values = g.Select(x => x.Value).Distinct().ToList();

                Dictionary<string, List<string>>? valueTranslations = null;

                if (name is "dlss" or "rgb" or "ray_tracing")
                {
                    valueTranslations = new()
                    {
                        { "uk", values.Select(v => v == "Yes" ? "Так" : "Ні").ToList() },
                        { "en", values }
                    };
                }
                else if (name == "warranty")
                {
                    valueTranslations = new()
                    {
                        { "uk", values.Select(v => v.Replace(" years", " роки")).ToList() },
                        { "en", values }
                    };
                }

                return new
                {
                    name,
                    type = "string",
                    values,
                    translations,
                    value_translations = valueTranslations
                };
            });

        var intFilters = new List<object>();

        var intGroups = allParamsInt.Models
            .Where(p => productIds.Contains(p.product_id))
            .GroupBy(p => p.Name.ToLower());

        foreach (var g in intGroups)
        {
            var name = g.Key;
            var translations = LocalizationHelper.ParameterTranslations.TryGetValue(name, out var tr) ? tr : null;

            var min = g.Min(x => x.Value);
            var max = g.Max(x => x.Value);

            if (name == "price")
            {
                var values = new List<string> { $"{min}-{max}" };
                var ukList = new List<string> { $"{min}-{max}₴" };
                var enList = new List<string> { $"{min}-{max}₴" };

                intFilters.Add(new
                {
                    name,
                    type = "int",
                    values,
                    translations,
                    value_translations = new Dictionary<string, List<string>>
            {
                { "uk", ukList },
                { "en", enList }
            }
                });

                continue;
            }

            var step = (int)Math.Ceiling((max - min + 1) / 5.0);
            var valuesRange = new List<string>();
            var ukValues = new List<string>();
            var enValues = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                int from = min + step * i;
                int to = Math.Min(from + step - 1, max);
                valuesRange.Add($"{from}-{to}");

                if (LocalizationHelper.ValueSuffixes.TryGetValue(name, out var suffix))
                {
                    ukValues.Add($"{from}-{to}{suffix.uk}");
                    enValues.Add($"{from}-{to}{suffix.en}");
                }
            }

            Dictionary<string, List<string>>? valueTranslations = null;
            if (ukValues.Any() && enValues.Any())
            {
                valueTranslations = new()
        {
            { "uk", ukValues },
            { "en", enValues }
        };
            }

            intFilters.Add(new
            {
                name,
                type = "int",
                values = valuesRange,
                translations,
                value_translations = valueTranslations
            });
        }

        var allFilters = stringFilters.Cast<object>().Concat(intFilters.Cast<object>()).ToList();
        return Ok(allFilters);
    }
}

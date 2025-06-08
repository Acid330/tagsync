using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/productfilter")]
public class ProductFilterController : ControllerBase
{
    [HttpGet("filter")]
    public async Task<IActionResult> FilterProducts()
    {
        var queryParams = HttpContext.Request.Query;
        var filters = new Dictionary<string, HashSet<string>>();
        var rangeFilters = new Dictionary<string, List<(int from, int to)>>();

        int page = queryParams.TryGetValue("page", out var pgVal) && int.TryParse(pgVal, out int pg) ? Math.Max(pg, 1) : 1;
        int limit = queryParams.TryGetValue("limit", out var limVal) && int.TryParse(limVal, out int lim) ? Math.Max(lim, 1) : 10;
        string sortBy = queryParams.TryGetValue("sort_by", out var sb) ? sb.ToString().ToLower() : "id";
        string sortOrder = queryParams.TryGetValue("sort_order", out var so) ? so.ToString().ToLower() : "asc";
        string category = queryParams.TryGetValue("category", out var catVal) ? catVal.ToString().ToLower() : "";

        foreach (var param in queryParams)
        {
            var key = param.Key.ToLower();
            var values = param.Value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (key.EndsWith("_range"))
            {
                var name = key.Replace("_range", "");
                rangeFilters[name] = new();
                foreach (var range in values)
                {
                    var bounds = range.Split('-');
                    if (bounds.Length == 2 && int.TryParse(bounds[0], out int from) && int.TryParse(bounds[1], out int to))
                        rangeFilters[name].Add((from, to));
                }
            }
            else if (key is not ("sort_by" or "sort_order" or "page" or "limit" or "category"))
            {
                filters[key] = values.Select(v => v.ToLower()).ToHashSet();
            }
        }

        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();

        var filteredProducts = allProducts.Models
            .Where(p => p.Category?.ToLower() == category)
            .ToList();

        var matchedProductIds = filteredProducts
            .Where(p =>
            {
                var productParams = allParams.Models.Where(m => m.product_id == p.Id).ToList();
                foreach (var filter in filters)
                {
                    var match = productParams.Any(pp =>
                        pp.Name.Equals(filter.Key, StringComparison.OrdinalIgnoreCase) &&
                        filter.Value.Any(val => pp.Value.ToLower().Contains(val)));
                    if (!match) return false;
                }
                return true;
            })
            .Select(p => p.Id)
            .ToHashSet();

        var numericMatchedIds = new HashSet<int>();
        foreach (var product in filteredProducts)
        {
            int id = product.Id;
            bool include = true;

            foreach (var rangeFilter in rangeFilters)
            {
                var param = allParamsInt.Models.FirstOrDefault(p => p.product_id == id && p.Name.ToLower() == rangeFilter.Key);
                if (param == null || !int.TryParse(param.Value.ToString(), out int val))
                {
                    include = false;
                    break;
                }

                bool inAnyRange = rangeFilter.Value.Any(r => val >= r.from && val <= r.to);
                if (!inAnyRange)
                {
                    include = false;
                    break;
                }
            }

            if (include)
                numericMatchedIds.Add(id);
        }

        if (rangeFilters.Count > 0)
            matchedProductIds = matchedProductIds.Intersect(numericMatchedIds).ToHashSet();

        var filtered = filteredProducts
            .Where(p => matchedProductIds.Contains(p.Id))
            .ToList();

        var results = filtered
            .Select(p => new
            {
                product = p,
                price = allParamsInt.Models.FirstOrDefault(x => x.product_id == p.Id && x.Name == "price")?.Value
            });

        results = sortBy switch
        {
            "price" => sortOrder == "desc" ? results.OrderByDescending(x => x.price) : results.OrderBy(x => x.price),
            "title" => sortOrder == "desc" ? results.OrderByDescending(x => x.product.Title) : results.OrderBy(x => x.product.Title),
            "views" => sortOrder == "desc" ? results.OrderByDescending(x => x.product.Views) : results.OrderBy(x => x.product.Views),
            _ => sortOrder == "desc" ? results.OrderByDescending(x => x.product.Id) : results.OrderBy(x => x.product.Id)
        };

        int totalCount = results.Count();

        results = results.Skip((page - 1) * limit).Take(limit);

        var response = results.Select(r =>
        {
            var ratings = allReviews.Models
                .Where(rvw => rvw.product_id == r.product.Id)
                .Select(rvw => rvw.average_rating)
                .ToList();

            float? averageRating = ratings.Count == 0
                ? null
                : (float)Math.Round(ratings.Average(), 1);

            return new
            {
                product_id = r.product.Id,
                title = r.product.Title,
                slug = r.product.Category?.ToLower(),
                translations_slug = LocalizationHelper.CategoryTranslations.TryGetValue(r.product.Category?.ToLower() ?? "", out var slugTr) ? slugTr : null,
                images = productImages.Models
                    .Where(img => img.product_id == r.product.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                price = r.price,
                views = r.product.Views,
                average_rating = averageRating,
                characteristics = allParamsInt.Models
                    .Where(param => param.product_id == r.product.Id)
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
                            .Where(param => param.product_id == r.product.Id)
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
                    ).ToList()
            };
        });

        return Ok(new
        {
            count_category = filteredProducts.Count,
            count_page = response.Count(),
            products = response
        });
        }
    }

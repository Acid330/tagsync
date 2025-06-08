using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int? page = null, [FromQuery] int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { error = "Query cannot be empty." });

        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();

        var normalizedQuery = query.Trim().ToLower();
        var allQueries = new List<string> { normalizedQuery };

        foreach (var (key, synonyms) in LocalizationHelper.SynonymMap)
        {
            if (synonyms.Any(s => s.ToLower() == normalizedQuery))
                allQueries.Add(key);
        }

        allQueries = allQueries.Distinct().ToList();

        var matchedProducts = allProducts.Models
            .Where(p => allQueries.Any(q =>
                (!string.IsNullOrEmpty(p.Title) && p.Title.ToLower().Contains(q)) ||
                (!string.IsNullOrEmpty(p.Category) && p.Category.ToLower().Contains(q))
            ))
            .ToList();

        int totalCount = matchedProducts.Count;

        if (page.HasValue && limit.HasValue)
        {
            int skip = (page.Value - 1) * limit.Value;
            matchedProducts = matchedProducts.Skip(skip).Take(limit.Value).ToList();
        }

        var results = matchedProducts.Select(p =>
        {
            var price = allParamsInt.Models.FirstOrDefault(x => x.product_id == p.Id && x.Name == "price")?.Value;
            var ratings = allReviews.Models.Where(r => r.product_id == p.Id).Select(r => r.rating).ToList();
            float? averageRating = ratings.Count == 0 ? null : (float)Math.Round(ratings.Average(), 1);

            return new
            {
                product_id = p.Id,
                title = p.Title,
                slug = p.Category?.ToLower(),
                translations_slug = LocalizationHelper.CategoryTranslations.TryGetValue(p.Category?.ToLower() ?? "", out var tr) ? tr : null,
                images = productImages.Models
                    .Where(img => img.product_id == p.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                price,
                rating = averageRating
            };
        });

        return Ok(new
        {
            count = totalCount,
            products = results
        });
    }


}

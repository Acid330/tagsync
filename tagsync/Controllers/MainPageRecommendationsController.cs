using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;
using static Supabase.Postgrest.Constants;

namespace tagsync.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MainPageRecommendationsController : ControllerBase
{
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularProducts([FromQuery] int limit = 10)
    {
        var allProducts = await SupabaseConnector.Client
            .From<Product>()
            .Order(p => p.Views, Ordering.Descending)
            .Limit(limit)
            .Get();

        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();

        var result = new List<RecommendedProductDto>();

        foreach (var product in allProducts.Models)
        {
            var priceParam = await SupabaseConnector.Client
                .From<ProductParameterInt>()
                .Filter(p => p.product_id, Operator.Equals, product.Id)
                .Filter(p => p.Name, Operator.Equals, "price")
                .Get();

            int price = priceParam.Models.FirstOrDefault()?.Value ?? 0;

            var productRatings = allReviews.Models
                .Where(r => r.product_id == product.Id)
                .Select(r => r.rating)
                .ToList();

            float? averageRating = productRatings.Count == 0
                ? null
                : (float)Math.Round(productRatings.Average(), 1);

            result.Add(new RecommendedProductDto
            {
                product_id = product.Id,
                Title = product.Title,
                Slug = product.Category.ToLower(),
                Category = product.Category,
                rating = averageRating,
                images = productImages.Models
                    .Where(img => img.product_id == product.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                Price = price,
                Views = product.Views
            });
        }

        return Ok(result);
    }



    [HttpGet("toprated")]
    public async Task<IActionResult> GetTopRatedProducts([FromQuery] int limit = 10)
    {
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();

        var productRatingsGrouped = allReviews.Models
            .GroupBy(r => r.product_id)
            .Select(g => new
            {
                product_id = g.Key,
                avg_rating = g.Average(r => r.rating)
            })
            .Where(r => r.avg_rating >= 4)
            .OrderByDescending(r => r.avg_rating)
            .Take(limit)
            .ToList();

        var result = new List<RecommendedProductDto>();

        foreach (var r in productRatingsGrouped)
        {
            var product = allProducts.Models.FirstOrDefault(p => p.Id == r.product_id);
            if (product == null)
                continue;

            var priceParam = await SupabaseConnector.Client
                .From<ProductParameterInt>()
                .Filter(p => p.product_id, Operator.Equals, product.Id)
                .Filter(p => p.Name, Operator.Equals, "price")
                .Get();

            int price = priceParam.Models.FirstOrDefault()?.Value ?? 0;

            var productRatings = allReviews.Models
                .Where(rv => rv.product_id == product.Id)
                .Select(rv => rv.rating) 
                .ToList();

            float? averageRating = productRatings.Count == 0
                ? null
                : (float)Math.Round(productRatings.Average(), 1);

            result.Add(new RecommendedProductDto
            {
                product_id = product.Id,
                Title = product.Title,
                Slug = product.Category.ToLower(),
                Category = product.Category,
                rating = averageRating,
                images = productImages.Models
                    .Where(img => img.product_id == product.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                Price = price,
                Views = product.Views
            });
        }

        return Ok(result);
    }

    [HttpGet("lastviewed")]
    public async Task<IActionResult> GetMostRecentlyViewedProducts([FromQuery] string email, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });

        var viewedProducts = await SupabaseConnector.Client
            .From<ViewedProduct>()
            .Filter(vp => vp.UserEmail, Operator.Equals, email)
            .Order(vp => vp.ViewedAt, Ordering.Descending)
            .Limit(limit)
            .Get();

        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();

        var result = new List<RecommendedProductDto>();

        foreach (var vp in viewedProducts.Models)
        {
            var product = allProducts.Models.FirstOrDefault(p => p.Id == vp.product_id);
            if (product == null)
                continue;

            // Получаем цену
            var priceParam = await SupabaseConnector.Client
                .From<ProductParameterInt>()
                .Filter(p => p.product_id, Operator.Equals, product.Id)
                .Filter(p => p.Name, Operator.Equals, "price")
                .Get();

            int price = priceParam.Models.FirstOrDefault()?.Value ?? 0;

            var productRatings = allReviews.Models
                .Where(r => r.product_id == product.Id)
                .Select(r => r.rating)
                .ToList();

            float? averageRating = productRatings.Count == 0
                ? null
                : (float)Math.Round(productRatings.Average(), 1);

            result.Add(new RecommendedProductDto
            {
                product_id = product.Id,
                Title = product.Title,
                Slug = product.Category.ToLower(),
                Category = product.Category,
                rating = averageRating,
                images = productImages.Models
                    .Where(img => img.product_id == product.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                Price = price,
                Views = product.Views
            });
        }

        return Ok(result);
    }

}

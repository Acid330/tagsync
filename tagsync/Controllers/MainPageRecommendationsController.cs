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

        var result = allProducts.Models
            .Select(product => new
            {
                product_id = product.Id,
                title = product.Title,
                slug = product.Category?.ToLower(),
                views = product.Views,
                images = productImages.Models
                    .Where(img => img.product_id == product.Id)
                    .Select(img => img.ImageUrl)
                    .ToList()
            })
            .ToList();

        return Ok(result);
    }


    [HttpGet("toprated")]
    public async Task<IActionResult> GetTopRatedProducts([FromQuery] int limit = 10)
    {
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();

        var topRatedProducts = allReviews.Models
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

        var result = topRatedProducts
            .Select(r =>
            {
                var product = allProducts.Models.FirstOrDefault(p => p.Id == r.product_id);
                if (product == null) return null;

                return new
                {
                    product_id = product.Id,
                    title = product.Title,
                    slug = product.Category?.ToLower(),
                    average_rating = (float)Math.Round(r.avg_rating, 1),
                    images = productImages.Models
                        .Where(img => img.product_id == product.Id)
                        .Select(img => img.ImageUrl)
                        .ToList()
                };
            })
            .Where(x => x != null)
            .ToList();

        return Ok(result);
    }


    [HttpGet("lastviewed")]
    public async Task<IActionResult> GetMostRecentlyViewedProducts([FromQuery] int limit = 10)
    {
        var viewedProducts = await SupabaseConnector.Client
            .From<ViewInsertModel>()
            .Order(vp => vp.ViewedAt, Ordering.Descending)
            .Limit(limit)
            .Get();

        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();

        var result = viewedProducts.Models
            .Select(vp =>
            {
                var product = allProducts.Models.FirstOrDefault(p => p.Id == vp.product_id);
                if (product == null) return null;

                return new
                {
                    product_id = product.Id,
                    title = product.Title,
                    slug = product.Category?.ToLower(),
                    viewed_at = vp.ViewedAt,
                    images = productImages.Models
                        .Where(img => img.product_id == product.Id)
                        .Select(img => img.ImageUrl)
                        .ToList()
                };
            })
            .Where(x => x != null)
            .ToList();

        return Ok(result);
    }


}

using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;
using static Supabase.Postgrest.Constants;

namespace tagsync.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    [HttpPost("add")]
    public async Task<IActionResult> AddReview([FromBody] AddReviewDto dto)
    {
        try
        {
            var client = SupabaseConnector.Client;

            var orders = await client
                .From<Order>()
                .Filter(o => o.UserEmail, Operator.Equals, dto.UserEmail)
                .Filter(o => o.ProductId, Operator.Equals, dto.ProductId)
                .Get();

            if (!orders.Models.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "You can only review products you have purchased."
                });
            }

            var existingReview = await client
                .From<ProductReview>()
                .Filter(r => r.UserEmail, Operator.Equals, dto.UserEmail)
                .Filter(r => r.ProductId, Operator.Equals, dto.ProductId)
                .Get();

            if (existingReview.Models.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "You have already submitted a review for this product."
                });
            }

            var review = new ProductReview
            {
                ProductId = dto.ProductId,
                UserEmail = dto.UserEmail,
                Rating = dto.Rating,
                Comment = dto.Comment,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var response = await client.From<ProductReview>().Insert(review);
            var inserted = response.Models.FirstOrDefault();

            return Ok(new
            {
                success = true,
                review = new
                {
                    inserted.Id,
                    inserted.ProductId,
                    inserted.UserEmail,
                    inserted.FirstName,
                    inserted.LastName,
                    inserted.Rating,
                    inserted.Comment,
                    inserted.CreatedAt
                }
            });
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message.Contains("violates foreign key constraint")
                    ? "No user with this email was found."
                    : "Error adding a review."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "System error: " + ex.Message
            });
        }
    }


    [HttpGet("{productId}")]
    public async Task<IActionResult> GetReviews(int productId)
    {
        var reviews = await SupabaseConnector.Client
            .From<ProductReview>()
            .Where(r => r.ProductId == productId)
            .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        return Ok(reviews.Models.Select(r => new
        {
            id = r.Id,
            productId = r.ProductId,
            userEmail = r.UserEmail,
            firstName = r.FirstName,
            lastName = r.LastName,
            rating = r.Rating,
            comment = r.Comment,
            createdAt = r.CreatedAt
        }));
    }
}

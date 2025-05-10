using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

[ApiController]
[Route("api/view")]
public class ViewTrackingController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RecordView([FromBody] ViewRequestDto dto)
    {
        var client = SupabaseConnector.Client;

        var view = new ViewInsertModel
        {
            UserEmail = dto.UserEmail,
            product_id = dto.product_id,
            ViewedAt = DateTime.UtcNow
        };

        await client.From<ViewInsertModel>().Insert(view);

        var products = await client.From<Product>().Get();
        var product = products.Models.FirstOrDefault(p => p.Id == dto.product_id);

        if (product != null)
        {
            product.Views += 1;
            await client.From<Product>().Update(product);
        }

        return Ok(new { success = true });
    }
}


using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;
using static Supabase.Postgrest.Constants;

namespace tagsync.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet("{email}")]
    public async Task<IActionResult> GetOrdersByEmail(string email)
    {
        var orders = await SupabaseConnector.Client
            .From<Order>()
            .Filter(o => o.UserEmail, Operator.Equals, email)
            .Get();

        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();

        var groupedOrders = orders.Models
            .GroupBy(o => o.CreatedAt)
            .Select(group =>
            {
                var first = group.First();

                var items = group.Select(order =>
                {
                    var product = allProducts.Models.FirstOrDefault(p => p.Id == order.product_id);
                    var priceParam = allParamsInt.Models.FirstOrDefault(p => p.product_id == order.product_id && p.Name.ToLower() == "price");
                    int? price = priceParam?.Value;

                    return new
                    {
                        order_time = first.CreatedAt,
                        product_id = order.product_id,
                        product_title = product?.Title,
                        Slug = product.Category.ToLower(),
                        images = productImages.Models
                            .Where(img => img.product_id == order.product_id)
                            .Select(img => img.ImageUrl)
                            .ToList(),
                        quantity = order.Quantity,
                        price_per_item = price,
                        total_price = price.HasValue ? price.Value * order.Quantity : (int?)null
                    };
                }).ToList();

                var totalOrderPrice = items
                    .Where(i => i.total_price.HasValue)
                    .Sum(i => i.total_price.Value);

                return new
                {
                    order_time = first.CreatedAt,
                    full_name = first.FullName,
                    phone = first.Phone,
                    city = first.City,
                    address = first.Address,
                    total_order_price = totalOrderPrice,
                    items
                };
            });

        return Ok(groupedOrders);
    }


    [HttpPost("checkout")]
    public async Task<IActionResult> CreateOrdersFromCart([FromBody] OrderFromCartRequest request)
    {
        var cartItems = await SupabaseConnector.Client
            .From<ShoppingCart>()
            .Filter(c => c.UserEmail, Operator.Equals, request.UserEmail)
            .Get();

        if (!cartItems.Models.Any())
            return BadRequest("Shopping cart empty");

        var orders = new List<Order>();

        foreach (var item in cartItems.Models)
        {
            orders.Add(new Order
            {
                UserEmail = request.UserEmail,
                FullName = request.FullName,
                Phone = request.Phone,
                City = request.City,
                Address = request.Address,
                product_id = item.product_id,
                Quantity = item.Quantity,
                CreatedAt = DateTime.UtcNow
            });
        }

        await SupabaseConnector.Client
            .From<Order>()
            .Insert(orders);

        foreach (var item in cartItems.Models)
        {
            await SupabaseConnector.Client
                .From<ShoppingCart>()
                .Delete(item);
        }

        return Ok(new
        {
            success = true,
            orderedCount = orders.Count,
            message = "Order placed successfully"
        });
    }


}

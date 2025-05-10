using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    public class CartDto
    {
        public string UserEmail { get; set;}
        public int product_id { get; set; }
        public int Quantity { get; set; } = 1;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] CartDto dto)
    {
        try
        {
            var existing = await SupabaseConnector.Client
                .From<ShoppingCart>()
                .Where(x => x.UserEmail == dto.UserEmail)
                .Where(x => x.product_id == dto.product_id)
                .Get();

            if (existing.Models.Count > 0)
            {
                var existingItem = existing.Models.First();
                existingItem.Quantity += dto.Quantity;
                await SupabaseConnector.Client.From<ShoppingCart>().Update(existingItem);
            }
            else
            {
                var item = new ShoppingCart
                {
                    UserEmail = dto.UserEmail,
                    product_id = dto.product_id,
                    Quantity = dto.Quantity,
                    AddedAt = DateTime.UtcNow
                };

                await SupabaseConnector.Client.From<ShoppingCart>().Insert(item);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }

        return Ok(new { success = true });
    }


    [HttpDelete("remove")]
    public async Task<IActionResult> Remove([FromBody] CartDto dto)
    {
        var existing = await SupabaseConnector.Client
            .From<ShoppingCart>()
            .Where(x => x.UserEmail == dto.UserEmail)
            .Where(x => x.product_id == dto.product_id)
            .Get();

        if (existing.Models.Count == 0)
            return NotFound(new { success = false, message = "Item not found in shopping cart." });

        var item = existing.Models.First();

        if (item.Quantity > dto.Quantity)
        {
            item.Quantity -= dto.Quantity;
            await SupabaseConnector.Client.From<ShoppingCart>().Update(item);
        }
        else
        {
            await SupabaseConnector.Client.From<ShoppingCart>().Delete(item);
        }

        return Ok(new { success = true });
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearCart([FromBody] ClearCartRequest request)
    {
        var client = SupabaseConnector.Client;

        var cartItems = await client.From<ShoppingCart>().Where(ci => ci.UserEmail == request.UserEmail).Get();

        if (cartItems.Models.Count == 0)
            return Ok(new { message = "The shopping cart is already empty" });

        foreach (var item in cartItems.Models)
        {
            await client.From<ShoppingCart>().Delete(item);
        }

        return Ok(new { message = "The shopping cart has been emptied" });
    }


    [HttpGet("{userEmail}")]
    public async Task<IActionResult> Get(string userEmail)
    {
        var cartItems = await SupabaseConnector.Client
            .From<ShoppingCart>()
            .Where(x => x.UserEmail == userEmail)
            .Get();

        var productIds = cartItems.Models.Select(c => c.product_id).ToHashSet();
        var allProducts = await SupabaseConnector.Client.From<Product>().Get();
        var allParams = await SupabaseConnector.Client.From<ProductParameter>().Get();
        var allParamsInt = await SupabaseConnector.Client.From<ProductParameterInt>().Get();
        var productImages = await SupabaseConnector.Client.From<ProductImage>().Get();
        var allReviews = await SupabaseConnector.Client.From<ProductReview>().Get();

        var cartProductList = new List<object>();
        decimal cartPrice = 0;

        foreach (var item in cartItems.Models)
        {
            var product = allProducts.Models.FirstOrDefault(p => p.Id == item.product_id);
            if (product == null) continue;

            var priceParam = allParamsInt.Models.FirstOrDefault(p => p.product_id == product.Id && p.Name == "price");
            var price = priceParam?.Value ?? 0;
            var allPrice = price * item.Quantity;
            cartPrice += allPrice;

            var characteristics = allParamsInt.Models
                .Where(param => param.product_id == product.Id)
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
                        .Where(param => param.product_id == product.Id)
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

            var slug = product.Category?.ToLower();
            var translations_slug = LocalizationHelper.CategoryTranslations.TryGetValue(slug, out var trCat) ? trCat : null;

            var ratings = allReviews.Models
                .Where(rvw => rvw.product_id == product.Id)
                .Select(rvw => rvw.average_rating)
                .ToList();

            float? averageRating = ratings.Count == 0
                ? null
                : (float)Math.Round(ratings.Average(), 1);

            cartProductList.Add(new
            {
                product_id = product.Id,
                title = product.Title,
                slug = product.Category?.ToLower(),
                translations_slug,
                average_rating = averageRating,
                images = productImages.Models
                    .Where(img => img.product_id == product.Id)
                    .Select(img => img.ImageUrl)
                    .ToList(),
                quantity = item.Quantity,
                price,
                all_price = allPrice,
                characteristics
            });
        }

        int totalQuantity = cartItems.Models.Sum(x => x.Quantity);

        return Ok(new
        {
            cart_price = cartPrice,
            total_quantity = totalQuantity,
            items = cartProductList
        });
    }
}

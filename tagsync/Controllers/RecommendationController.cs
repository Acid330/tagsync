using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;
using static Supabase.Postgrest.Constants;

namespace tagsync.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public RecommendationController()
    {
        _supabase = SupabaseConnector.Client;
    }
    private async Task<int> GetProductPrice(int productId)
    {
        var priceParam = await _supabase
            .From<ProductParameterInt>()
            .Filter(p => p.ProductId, Operator.Equals, productId)
            .Filter(p => p.Name, Operator.Equals, "price")
            .Get();

        return priceParam.Models.FirstOrDefault()?.Value ?? 0;
    }


    [HttpGet("similar/{productId}")]
    public async Task<IActionResult> GetSimilarProducts(int productId, int page = 1, int limit = 10)
    {
        var productResponse = await _supabase
            .From<Product>()
            .Filter(p => p.Id, Operator.Equals, productId)
            .Get();

        var currentProduct = productResponse.Models.FirstOrDefault();
        if (currentProduct == null)
            return NotFound("Product not found");

        var strParams = await _supabase
            .From<ProductParameter>()
            .Filter(p => p.ProductId, Operator.Equals, productId)
            .Get();

        var intParams = await _supabase
            .From<ProductParameterInt>()
            .Filter(p => p.ProductId, Operator.Equals, productId)
            .Get();

        var currentParamSet = new HashSet<string>(
            strParams.Models.Select(p => $"{p.Name}:{p.Value}")
            .Concat(intParams.Models.Select(p => $"{p.Name}:{p.Value}"))
        );

        var otherProducts = await _supabase
            .From<Product>()
            .Filter(p => p.Category, Operator.Equals, currentProduct.Category)
            .Get();

        var similarityList = new List<(Product, int)>();

        foreach (var prod in otherProducts.Models)
        {
            if (prod.Id == productId)
                continue;

            var pParams = await _supabase
                .From<ProductParameter>()
                .Filter(p => p.ProductId, Operator.Equals, prod.Id)
                .Get();

            var pIntParams = await _supabase
                .From<ProductParameterInt>()
                .Filter(p => p.ProductId, Operator.Equals, prod.Id)
                .Get();

            var paramSet = new HashSet<string>(
                pParams.Models.Select(p => $"{p.Name}:{p.Value}")
                .Concat(pIntParams.Models.Select(p => $"{p.Name}:{p.Value}"))
            );

            int similarityScore = currentParamSet.Intersect(paramSet).Count();
            similarityList.Add((prod, similarityScore));
        }

        var pagedSimilar = similarityList
            .OrderByDescending(p => p.Item2)
            .Skip((page - 1) * limit)
            .Take(limit);

        var result = new List<RecommendedProductDto>();

        foreach (var (product, _) in pagedSimilar)
        {
            int price = await GetProductPrice(product.Id);

            result.Add(new RecommendedProductDto
            {
                ProductId = product.Id,
                Title = product.Title,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                Price = price
            });
        }

        return Ok(result);
    }


    [HttpGet("compatible/{productId}")]
    public async Task<IActionResult> GetCompatibleProducts(int productId, int page = 1, int limit = 10)
    {
        var productResponse = await _supabase
            .From<Product>()
            .Filter(p => p.Id, Operator.Equals, productId)
            .Get();

        var product = productResponse.Models.FirstOrDefault();
        if (product == null)
            return NotFound("Product not found");

        var result = new List<RecommendedProductDto>();

        async Task<string?> GetParam(int pid, string name)
        {
            var param = await _supabase
                .From<ProductParameter>()
                .Filter(p => p.ProductId, Operator.Equals, pid)
                .Filter(p => p.Name, Operator.Equals, name)
                .Get();

            return param.Models.FirstOrDefault()?.Value;
        }

        async Task<int?> GetIntParam(int pid, string name)
        {
            var param = await _supabase
                .From<ProductParameterInt>()
                .Filter(p => p.ProductId, Operator.Equals, pid)
                .Filter(p => p.Name, Operator.Equals, name)
                .Get();

            return param.Models.FirstOrDefault()?.Value;
        }

        async Task AddIfCompatible(Product other)
        {
            int price = await GetProductPrice(other.Id);
            result.Add(new RecommendedProductDto
            {
                ProductId = other.Id,
                Title = other.Title,
                Category = other.Category,
                ImageUrl = other.ImageUrl,
                Price = price
            });
        }

        if (product.Category == "CPU")
        {
            var socket = await GetParam(product.Id, "socket");
            var mobos = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Motherboard").Get();
            foreach (var m in mobos.Models)
                if ((await GetParam(m.Id, "socket")) == socket)
                    await AddIfCompatible(m);
        }
        else if (product.Category == "Motherboard")
        {
            var socket = await GetParam(product.Id, "socket");
            var ramType = await GetParam(product.Id, "ram_type");
            var interfaceVal = await GetParam(product.Id, "interface");

            var cpus = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "CPU").Get();
            foreach (var c in cpus.Models)
                if ((await GetParam(c.Id, "socket")) == socket)
                    await AddIfCompatible(c);

            var rams = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "RAM").Get();
            foreach (var r in rams.Models)
                if ((await GetParam(r.Id, "ram_type")) == ramType)
                    await AddIfCompatible(r);

            var storage = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Storage").Get();
            foreach (var s in storage.Models)
                if ((await GetParam(s.Id, "interface")) == interfaceVal)
                    await AddIfCompatible(s);
        }
        else if (product.Category == "RAM")
        {
            var ramType = await GetParam(product.Id, "ram_type");
            var mobos = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Motherboard").Get();
            foreach (var m in mobos.Models)
                if ((await GetParam(m.Id, "ram_type")) == ramType)
                    await AddIfCompatible(m);
        }
        else if (product.Category == "Storage")
        {
            var iface = await GetParam(product.Id, "interface");
            var mobos = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Motherboard").Get();
            foreach (var m in mobos.Models)
                if ((await GetParam(m.Id, "interface")) == iface)
                    await AddIfCompatible(m);
        }
        else if (product.Category == "GPU")
        {
            var iface = await GetParam(product.Id, "interface");
            var length = await GetIntParam(product.Id, "length");
            var power = await GetIntParam(product.Id, "recommended_psu");

            var mobos = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Motherboard").Get();
            foreach (var m in mobos.Models)
                if ((await GetParam(m.Id, "interface")) == iface)
                    await AddIfCompatible(m);

            var cases = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Case").Get();
            foreach (var c in cases.Models)
                if ((await GetIntParam(c.Id, "max_gpu_length")) is int maxLen && length <= maxLen)
                    await AddIfCompatible(c);

            var psus = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "PSU").Get();
            foreach (var p in psus.Models)
                if ((await GetIntParam(p.Id, "power")) is int w && power <= w)
                    await AddIfCompatible(p);
        }
        else if (product.Category == "Cooler")
        {
            var height = await GetIntParam(product.Id, "height");
            var tdp = await GetIntParam(product.Id, "tdp_support");
            var socket = await GetParam(product.Id, "socket_support");

            var cpus = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "CPU").Get();
            foreach (var cpu in cpus.Models)
            {
                var cpuSocket = await GetParam(cpu.Id, "socket");
                var cpuTdp = await GetIntParam(cpu.Id, "tdp");

                if (cpuSocket != null && socket?.Contains(cpuSocket) == true && cpuTdp <= tdp)
                    await AddIfCompatible(cpu);
            }

            var cases = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Case").Get();
            foreach (var c in cases.Models)
                if ((await GetIntParam(c.Id, "max_cooler_height")) is int h && height <= h)
                    await AddIfCompatible(c);
        }
        else if (product.Category == "Case")
        {
            var maxGpu = await GetIntParam(product.Id, "max_gpu_length");
            var maxCooler = await GetIntParam(product.Id, "max_cooler_height");
            var formFactor = await GetParam(product.Id, "mb_form_factor");

            var gpus = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "GPU").Get();
            foreach (var g in gpus.Models)
                if ((await GetIntParam(g.Id, "length")) is int l && l <= maxGpu)
                    await AddIfCompatible(g);

            var coolers = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Cooler").Get();
            foreach (var cl in coolers.Models)
                if ((await GetIntParam(cl.Id, "height")) is int h && h <= maxCooler)
                    await AddIfCompatible(cl);

            var mobos = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "Motherboard").Get();
            foreach (var m in mobos.Models)
                if ((await GetParam(m.Id, "mb_form_factor")) == formFactor)
                    await AddIfCompatible(m);
        }
        else if (product.Category == "PSU")
        {
            var power = await GetIntParam(product.Id, "power");

            var gpus = await _supabase.From<Product>().Filter(p => p.Category, Operator.Equals, "GPU").Get();
            foreach (var g in gpus.Models)
                if ((await GetIntParam(g.Id, "recommended_psu")) is int draw && draw <= power)
                    await AddIfCompatible(g);
        }
        var paged = result
    .Skip((page - 1) * limit)
    .Take(limit)
    .ToList();

        return Ok(paged);
    }


    [HttpGet("price-based/{productId}")]
    public async Task<IActionResult> GetPriceBasedRecommendations(int productId, int page = 1, int limit = 10)
    {
        var productResponse = await _supabase
            .From<Product>()
            .Filter(p => p.Id, Operator.Equals, productId)
            .Get();

        var product = productResponse.Models.FirstOrDefault();
        if (product == null)
            return NotFound("Product not found");

        var priceParam = await _supabase
            .From<ProductParameterInt>()
            .Filter(p => p.ProductId, Operator.Equals, product.Id)
            .Filter(p => p.Name, Operator.Equals, "price")
            .Get();

        var price = priceParam.Models.FirstOrDefault()?.Value ?? 0;

        int minPrice = (int)(price * 0.8);
        int maxPrice = (int)(price * 1.2);

        var allProducts = await _supabase
            .From<Product>()
            .Filter(p => p.Category, Operator.Equals, product.Category)
            .Get();

        var result = new List<RecommendedProductDto>();

        foreach (var p in allProducts.Models)
        {
            if (p.Id == product.Id)
                continue;

            var priceParamOther = await _supabase
                .From<ProductParameterInt>()
                .Filter(pp => pp.ProductId, Operator.Equals, p.Id)
                .Filter(pp => pp.Name, Operator.Equals, "price")
                .Get();

            var otherPrice = priceParamOther.Models.FirstOrDefault()?.Value ?? 0;

            if (otherPrice >= minPrice && otherPrice <= maxPrice)
            {
                result.Add(new RecommendedProductDto
                {
                    ProductId = p.Id,
                    Title = p.Title,
                    Category = p.Category,
                    ImageUrl = p.ImageUrl,
                    Price = otherPrice
                });
            }
        }
        var paged = result
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        return Ok(paged);

    }


    [HttpGet("also-viewed/{productId}")]
    public async Task<IActionResult> GetAlsoViewedProducts(int productId, int page = 1, int limit = 10)

    {
        var viewersResponse = await _supabase
            .From<ViewedProduct>()
            .Filter(p => p.ProductId, Operator.Equals, productId)
            .Get();

        var viewerEmails = viewersResponse.Models.Select(v => v.UserEmail).Distinct().ToList();

        if (!viewerEmails.Any())
            return Ok(new List<RecommendedProductDto>());

        var alsoViewedRaw = new List<ViewedProduct>();

        foreach (var email in viewerEmails)
        {
            var views = await _supabase
                .From<ViewedProduct>()
                .Filter(p => p.UserEmail, Operator.Equals, email)
                .Get();

            alsoViewedRaw.AddRange(views.Models);
        }

        var grouped = alsoViewedRaw
            .Where(v => v.ProductId != productId)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToList();

        var result = new List<RecommendedProductDto>();

        foreach (var group in grouped)
        {
            var productResponse = await _supabase
                .From<Product>()
                .Filter(p => p.Id, Operator.Equals, group.ProductId)
                .Get();

            var prod = productResponse.Models.FirstOrDefault();
            if (prod == null)
                continue;

            int price = await GetProductPrice(prod.Id);

            result.Add(new RecommendedProductDto
            {
                ProductId = prod.Id,
                Title = prod.Title,
                Category = prod.Category,
                ImageUrl = prod.ImageUrl,
                Price = price
            });
        }
        var paged = result
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        return Ok(paged);

    }


}


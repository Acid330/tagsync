using Microsoft.AspNetCore.Mvc;
using tagsync.Helpers;
using tagsync.Models;

namespace tagsync.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly Supabase.Client _supabaseClient;

    public UploadController()
    {
        _supabaseClient = SupabaseConnector.Client;
    }

    [HttpPost("upload-image-file")]
    public async Task<IActionResult> UploadProductImages([FromForm] int productId, [FromForm] List<IFormFile> image)
    {
        if (image == null || image.Count == 0)
            return BadRequest("No images have been transferred");

        var uploadedUrls = new List<string>();

        foreach (var file in image)
        {
            var fileExt = Path.GetExtension(file.FileName);
            var newFileName = $"{Guid.NewGuid()}{fileExt}";
            var path = $"product-images/{productId}/{newFileName}";

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            var uploadedPath = await _supabaseClient.Storage
                .From("images")
                .Upload(fileBytes, path, new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    ContentType = file.ContentType
                });

            if (string.IsNullOrEmpty(uploadedPath))
                return StatusCode(500, "File upload error");

            var publicUrl = _supabaseClient.Storage
                .From("images")
                .GetPublicUrl(path);


            uploadedUrls.Add(publicUrl);

            var img = new ProductImage
            {
                product_id = productId,
                ImageUrl = publicUrl
            };

            await _supabaseClient.From<ProductImage>().Insert(img);
        }

        return Ok(new
        {
            message = "Files successfully uploaded",
            urls = uploadedUrls
        });
    }
}

namespace tagsync.Models;

public class RecommendedProductDto
{
    public int product_id { get; set; }
    public string Title { get; set; }
    public string Category { get; set; }
    public List<string> images { get; set; }
    public int Price { get; set; }
    public float? rating { get; set; }
    public string Slug { get; set; }
    public int Views { get; set; }
}

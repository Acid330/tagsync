namespace tagsync.Models;

public class RecommendedProductDto
{
    public int ProductId { get; set; }
    public string Title { get; set; }
    public string Category { get; set; }
    public List<string> images { get; set; }
    public int Price { get; set; }
}

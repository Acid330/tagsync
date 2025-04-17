using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models;

[Table("products")]
public class Product : BaseModel
{
    [PrimaryKey("id", false), Column("id")]
    public int Id { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("category")]
    public string Category { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("views")]
    public int Views { get; set; }
}

[Table("product_parameters")]
public class ProductParameter : BaseModel
{
    [PrimaryKey("id", false), Column("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("value")]
    public string Value { get; set; }
}

[Table("product_parameters_int")]
public class ProductParameterInt : BaseModel
{
    [PrimaryKey("id", false), Column("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("value")]
    public int Value { get; set; }
}

[Table("product_reviews")]
public class ProductReview : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class AddReviewDto
{
    public int ProductId { get; set; }
    public string UserEmail { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

[Table("viewed_products")]
public class ViewedProduct : BaseModel
{
    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }
}


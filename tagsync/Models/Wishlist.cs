using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models;

[Table("wishlist")]
public class Wishlist : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; }
}
public class AddToWishlistDto
{
    public string UserEmail { get; set; }
    public int ProductId { get; set; }
}

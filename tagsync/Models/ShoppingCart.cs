using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models;

[Table("shopping_cart")]
public class ShoppingCart : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; }
}


public class ClearCartRequest
{
    public string UserEmail { get; set; }
}

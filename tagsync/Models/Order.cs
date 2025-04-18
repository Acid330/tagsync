using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models;

[Table("orders")]
public class Order : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("full_name")]
    public string FullName { get; set; }

    [Column("phone")]
    public string Phone { get; set; }

    [Column("city")]
    public string City { get; set; }

    [Column("address")]
    public string Address { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
public class OrderRequest
{
    public string UserEmail { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string City { get; set; }
    public string Address { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } 
}
public class OrderFromCartRequest
{
    public string UserEmail { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string City { get; set; }
    public string Address { get; set; }
}
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models;

[Table("viewed_products")]
public class ViewInsertModel : BaseModel
{
    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("product_id")]
    public int product_id { get; set; }

    [Column("viewed_at")]
    public DateTime ViewedAt { get; set; }
}
public class ViewRequestDto
{
    public string UserEmail { get; set; }
    public int product_id { get; set; }
}

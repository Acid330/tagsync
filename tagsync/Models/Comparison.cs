using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models;

[Table("comparisons")]
public class Comparison : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("product_id")]
    public int product_id { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; }
}

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace tagsync.Models
{
    [Table("product_images")]
    public class ProductImage : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("product_id")]
        public long product_id { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}

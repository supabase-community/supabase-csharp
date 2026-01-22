using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RealtimeExample.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = null!;

        [Column("name")]
        public string Name { get; set; } = null!;
    }
}

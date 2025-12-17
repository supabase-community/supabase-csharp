using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SupabaseTests.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [JsonProperty("username")]
        public string Username { get; set; }
    }
}

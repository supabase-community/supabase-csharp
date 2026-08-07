using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Supabase.Tests.Models;

[Table("users")]
public class User : BaseModel
{
    [JsonProperty("username")]
    public string Username { get; set; }
}

using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Supabase.Tests.Models;

[Table("users")]
public class User : BaseModel
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

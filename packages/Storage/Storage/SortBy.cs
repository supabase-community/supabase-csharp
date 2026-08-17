using System.Text.Json.Serialization;

namespace Supabase.Storage;

public class SortBy
{
    [JsonPropertyName("column")]
    public string? Column { get; set; } = "name";

    [JsonPropertyName("order")]
    public string? Order { get; set; } = "asc";
}

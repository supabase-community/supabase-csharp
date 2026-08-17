using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Storage;

public class FileOptions
{
    [JsonPropertyName("cacheControl")]
    public string CacheControl { get; set; } = "3600";

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "text/plain;charset=UTF-8";

    [JsonPropertyName("upsert")]
    public bool Upsert { get; set; }

    [JsonPropertyName("duplex")]
    public string? Duplex { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }
}

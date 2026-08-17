using System.Text.Json.Serialization;

namespace Supabase.Storage.Responses;

internal class CreatedUploadSignedUrlResponse
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}


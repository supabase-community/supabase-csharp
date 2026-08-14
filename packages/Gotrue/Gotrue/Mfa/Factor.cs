using System;
using System.Text.Json.Serialization;

namespace Supabase.Gotrue.Mfa;

public class Factor
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("friendly_name")]
    public string? FriendlyName { get; set; }

    [JsonPropertyName("factor_type")]
    public string FactorType { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

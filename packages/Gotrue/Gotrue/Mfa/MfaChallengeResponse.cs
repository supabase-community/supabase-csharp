using System.Text.Json.Serialization;

namespace Supabase.Gotrue.Mfa;

public class MfaChallengeResponse
{
    // ID of the newly created challenge.
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}

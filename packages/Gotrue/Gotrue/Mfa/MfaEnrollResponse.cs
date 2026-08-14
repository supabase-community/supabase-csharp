using System.Text.Json.Serialization;

namespace Supabase.Gotrue.Mfa;

public class MfaEnrollResponse
{
    // ID of the factor that was just enrolled (in an unverified state).
    [JsonPropertyName("id")]
    public string Id { get; set; }

    // Type of MFA factor. Only `totp` supported for now.
    [JsonPropertyName("type")]
    public string Type { get; set; }

    // TOTP enrollment information.
    [JsonPropertyName("totp")]
    public TOTP Totp { get; set; }

    // Friendly name of the factor, useful for distinguishing between factors
    [JsonPropertyName("friendly_name")]
    public string FriendlyName { get; set; }
}

using System.Text.Json.Serialization;

namespace Supabase.Gotrue.Mfa;

public class MfaUnenrollResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

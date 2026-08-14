using System.Text.Json.Serialization;

namespace Supabase.Gotrue.Mfa;

public class MfaVerifyResponse
{
    // New access token (JWT) after successful verification
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    //  Type of token, typically `Bearer`
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    // Number of seconds in which the access token will expire
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    // Refresh token you can use to obtain new access tokens when expired
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    // Updated user profile
    [JsonPropertyName("user")]
    public User User { get; set; }
}

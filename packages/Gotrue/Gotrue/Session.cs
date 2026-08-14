using System;
using System.Text.Json.Serialization;

#pragma warning disable CS1591

namespace Supabase.Gotrue;

/// <summary>
/// Represents a Gotrue Session
/// </summary>
public class Session
{
    /// <summary>
    /// The access token jwt. It is recommended to set the JWT_EXPIRY to a shorter expiry value.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// The number of seconds until the access token expires (since it was issued). Returned when a login is confirmed.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; }

    /// <summary>
    /// The oauth provider token. If present, this can be used to make external API requests to the oauth provider used.
    /// </summary>
    [JsonPropertyName("provider_token")]
    public string? ProviderToken { get; set; }

    /// <summary>
    /// The oauth provider refresh token. If present, this can be used to refresh the provider_token via the oauth provider's API.
    /// Not all oauth providers return a provider refresh token. If the provider_refresh_token is missing, please refer to the oauth provider's documentation for information on how to obtain the provider refresh token.
    /// </summary>
    [JsonPropertyName("provider_refresh_token")]
    public string? ProviderRefreshToken { get; set; }

    /// <summary>
    /// A one-time used refresh token that never expires.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

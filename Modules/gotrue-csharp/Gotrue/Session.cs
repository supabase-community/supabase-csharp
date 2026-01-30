using System;
using Newtonsoft.Json;

#pragma warning disable CS1591

namespace Supabase.Gotrue
{
	/// <summary>
	/// Represents a Gotrue Session
	/// </summary>
	public class Session
	{
		/// <summary>
		/// The access token jwt. It is recommended to set the JWT_EXPIRY to a shorter expiry value.
		/// </summary>
		[JsonProperty("access_token")]
		public string? AccessToken { get; set; }

		/// <summary>
		/// The number of seconds until the token expires (since it was issued). Returned when a login is confirmed.
		/// </summary>
		[JsonProperty("expires_in")]
		public long ExpiresIn { get; set; }

		/// <summary>
		/// The oauth provider token. If present, this can be used to make external API requests to the oauth provider used.
		/// </summary>
		[JsonProperty("provider_token")]
		public string? ProviderToken { get; set; }

		/// <summary>
		/// The oauth provider refresh token. If present, this can be used to refresh the provider_token via the oauth provider's API.
		/// Not all oauth providers return a provider refresh token. If the provider_refresh_token is missing, please refer to the oauth provider's documentation for information on how to obtain the provider refresh token.
		/// </summary>
		[JsonProperty("provider_refresh_token")]
		public string? ProviderRefreshToken { get; set; }

		/// <summary>
		/// A one-time used refresh token that never expires.
		/// </summary>
		[JsonProperty("refresh_token")]
		public string? RefreshToken { get; set; }

		[JsonProperty("token_type")]
		public string? TokenType { get; set; }

		[JsonProperty("user")]
		public User? User { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// The expiration date of this session, in UTC time.
		/// </summary>
		/// <returns></returns>
		public DateTime ExpiresAt() => new DateTimeOffset(CreatedAt).AddSeconds(ExpiresIn).ToUniversalTime().DateTime;

		/// <summary>
		/// Returns true if the session has expired
		/// </summary>
		/// <returns></returns>
		public bool Expired() => ExpiresAt() < DateTime.UtcNow;
	}
}
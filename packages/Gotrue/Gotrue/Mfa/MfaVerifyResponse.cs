using Newtonsoft.Json;

namespace Supabase.Gotrue.Mfa
{
	public class MfaVerifyResponse
	{
		// New access token (JWT) after successful verification
		[JsonProperty("access_token")]
		public string AccessToken { get; set; }

		//  Type of token, typically `Bearer`
		[JsonProperty("token_type")]
		public string TokenType { get; set; }

		// Number of seconds in which the access token will expire
		[JsonProperty("expires_in")]
		public int ExpiresIn { get; set; }

		// Refresh token you can use to obtain new access tokens when expired
		[JsonProperty("refresh_token")]
		public string RefreshToken { get; set; }

		// Updated user profile
		[JsonProperty("user")]
		public User User { get; set; }
	}
}
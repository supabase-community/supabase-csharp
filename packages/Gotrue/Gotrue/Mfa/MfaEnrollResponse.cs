using Newtonsoft.Json;

namespace Supabase.Gotrue.Mfa
{
	public class MfaEnrollResponse
	{
		// ID of the factor that was just enrolled (in an unverified state).
		[JsonProperty("id")]
		public string Id { get; set; }

		// Type of MFA factor. Only `totp` supported for now.
		[JsonProperty("type")]
		public string Type { get; set; }

		// TOTP enrollment information.
		[JsonProperty("totp")]
		public TOTP Totp { get; set; }

		// Friendly name of the factor, useful for distinguishing between factors
		[JsonProperty("friendly_name")]
		public string FriendlyName { get; set; }
	}
}
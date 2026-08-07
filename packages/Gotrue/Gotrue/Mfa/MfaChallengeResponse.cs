using Newtonsoft.Json;

namespace Supabase.Gotrue.Mfa
{
	public class MfaChallengeResponse
	{
		// ID of the newly created challenge.
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("expires_at")]
		public long ExpiresAt { get; set; }
	}
}
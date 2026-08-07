using Newtonsoft.Json;

namespace Supabase.Gotrue.Mfa
{
	public class MfaUnenrollResponse
	{
		[JsonProperty("id")]
		public string Id { get; set; }
	}
}
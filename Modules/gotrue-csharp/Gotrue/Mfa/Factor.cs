using System;
using Newtonsoft.Json;

namespace Supabase.Gotrue.Mfa
{
	public class Factor
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("friendly_name")]
		public string? FriendlyName { get; set; }

		[JsonProperty("factor_type")]
		public string FactorType { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }

		[JsonProperty("updated_at")]
		public DateTime UpdatedAt { get; set; }
	}
}
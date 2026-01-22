using Newtonsoft.Json;
using Supabase.Realtime.Models;

namespace PresenceExample.Models
{
	class Message : BaseBroadcast
	{
		[JsonProperty("color")]
		public string? Color { get; set; }

		[JsonProperty("userId")]
		public string? UserId { get; set; }

		[JsonProperty("content")]
		public string? Content { get; set; }

		[JsonProperty("createdAt")]
		public DateTime CreatedAt { get; set; }
	}
}

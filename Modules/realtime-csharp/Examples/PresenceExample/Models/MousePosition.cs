using Newtonsoft.Json;
using Supabase.Realtime.Models;

namespace PresenceExample.Models
{
	class MousePosition : BaseBroadcast
	{
		[JsonProperty("color")]
		public string? Color { get; set; }

		[JsonProperty("userId")]
		public string? UserId { get; set; }

		[JsonProperty("mouseX")]
		public double MouseX { get; set; }

		[JsonProperty("mouseY")]
		public double MouseY { get; set; }

		[JsonIgnore]
		public DateTime AddedAt { get; set; }
	}
}

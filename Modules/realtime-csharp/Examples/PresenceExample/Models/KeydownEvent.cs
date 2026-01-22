namespace PresenceExample.Models
{
	public class KeydownEvent
	{
		public bool altKey { get; set; }
		public bool button { get; set; }
		public bool buttons { get; set; }
		public bool ctrlKey { get; set; }
		public bool metaKey { get; set; }
		public bool shiftKey { get; set; }

		public string? code { get; set; }
		public string? key { get; set; }

		public int keyCode { get; set; }
	}
}

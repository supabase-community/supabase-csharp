using Newtonsoft.Json;

namespace Supabase.Storage.Responses
{
	internal class CreatedUploadSignedUrlResponse
	{
		[JsonProperty("url")]
		public string? Url { get; set; }
	}
}


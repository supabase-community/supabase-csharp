using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class CreateSignedUrlResponse
    {
        [JsonProperty("signedURL")]
        public string? SignedUrl { get; set; }
    }

    public class CreateSignedUrlsResponse: CreateSignedUrlResponse
    {
        [JsonProperty("path")]
        public string? Path { get; set; }
    }
}

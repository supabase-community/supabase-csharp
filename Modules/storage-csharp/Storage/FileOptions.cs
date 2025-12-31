using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class FileOptions
    {
        [JsonProperty("cacheControl")]
        public string CacheControl { get; set; } = "3600";

        [JsonProperty("contentType")]
        public string ContentType { get; set; } = "text/plain;charset=UTF-8";

        [JsonProperty("upsert")]
        public bool Upsert { get; set; }
        
        [JsonProperty("duplex")]
        public string? Duplex { get; set; }
        
        [JsonProperty("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
        
        [JsonProperty("headers")]
        public Dictionary<string, string>? Headers { get; set; }
    }
}

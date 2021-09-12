using System;
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
        public bool Upsert { get; set; } = false;
    }
}

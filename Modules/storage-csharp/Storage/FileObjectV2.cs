using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class FileObjectV2
    {
        
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("bucket_id")]
        public string? BucketId { get; set; }

        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("last_accessed_at")]
        public DateTime? LastAccessedAt { get; set; }
        
        [JsonProperty("size")]
        public int? Size { get; set; }
        
        [JsonProperty("cache_control")]
        public string? CacheControl { get; set; }
        
        [JsonProperty("content_type")]
        public string? ContentType { get; set; }
        
        [JsonProperty("etag")]
        public string? Etag { get; set; }
        
        [JsonProperty("last_modified")]
        public DateTime? LastModified { get; set; }
        
        [JsonProperty("metadata")]
        public Dictionary<string, string>? Metadata { get; set; } 
    }
}

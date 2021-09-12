using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class FileObject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("bucket_id")]
        public string BucketId { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("last_accessed_at")]
        public DateTime LastAccessedAt { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> MetaData = new Dictionary<string, object>();

        [JsonProperty("buckets")]
        public Bucket Buckets { get; set; }
    }
}

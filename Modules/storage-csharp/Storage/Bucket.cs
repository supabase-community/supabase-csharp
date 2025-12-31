using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class Bucket
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("owner")]
        public string? Owner { get; set; }

        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// The visibility of the bucket. Public buckets don't require an authorization token to download objects,
		/// but still require a valid token for all other operations. By default, buckets are private.
        /// </summary>
        [JsonProperty("public")]
        public bool Public { get; set; }

        /// <summary>
        /// Specifies the file size limit that this bucket can accept during upload.
        ///
        /// Expects a string value following a format like: '1kb', '50mb', '150kb', etc.
        /// </summary>
        [JsonProperty("file_size_limit", NullValueHandling = NullValueHandling.Include)]
        public string? FileSizeLimit { get; set; }

        /// <summary>
        /// Specifies the allowed mime types that this bucket can accept during upload.
		///
		/// Expects a List of values such as: ['image/jpeg', 'image/png', etc]
        /// </summary>
        [JsonProperty("allowed_mime_types", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? AllowedMimes { get; set; }
    }
}

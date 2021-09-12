using System;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class CreateSignedUrlResponse
    {
        [JsonProperty("signedURL")]
        public string SignedUrl { get; set; }
    }
}

using System;
using Newtonsoft.Json;

namespace Supabase.Auth
{
    public class ErrorResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}

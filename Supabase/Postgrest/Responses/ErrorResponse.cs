using System;
using Newtonsoft.Json;

namespace Supabase.Postgrest.Responses
{
    public class ErrorResponse : BaseResponse
    {
        [JsonProperty("hint")]
        public object Hint { get; set; }

        [JsonProperty("details")]
        public object Details { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}

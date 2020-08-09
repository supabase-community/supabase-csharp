using System;
using Newtonsoft.Json;

namespace Supabase.Postgrest.Responses
{
    public class ErrorResponse : BaseResponse
    {
        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }
    }
}

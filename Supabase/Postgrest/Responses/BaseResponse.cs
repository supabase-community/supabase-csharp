using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace Supabase.Postgrest.Responses
{
    public class BaseResponse
    {
        [JsonIgnore]
        public HttpResponseMessage ResponseMessage { get; set; }

        [JsonIgnore]
        public string Content { get; set; }
    }
}

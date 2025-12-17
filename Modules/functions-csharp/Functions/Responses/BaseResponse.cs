using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace Supabase.Functions.Responses
{
    /// <summary>
    /// A wrapper class from which all Responses derive.
    /// </summary>
    public class BaseResponse
    {
        /// <summary>
        /// The response message
        /// </summary>
        [JsonIgnore]
        public HttpResponseMessage? ResponseMessage { get; set; }

        /// <summary>
        /// The response content.
        /// </summary>
        [JsonIgnore]
        public string? Content { get; set; }
    }
}

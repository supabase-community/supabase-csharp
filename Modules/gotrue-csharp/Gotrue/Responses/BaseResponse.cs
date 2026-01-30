using System.Net.Http;
using Newtonsoft.Json;
namespace Supabase.Gotrue.Responses
{
    /// <summary>
    /// A wrapper class from which all Responses derive.
    /// </summary>
    public class BaseResponse
    {
        /// <summary>
        /// The HTTP response message.
        /// </summary>
        [JsonIgnore]
        public HttpResponseMessage? ResponseMessage { get; set; }

        /// <summary>
        /// The HTTP response content as a string.
        /// </summary>
        [JsonIgnore]
        public string? Content { get; set; }
    }
}

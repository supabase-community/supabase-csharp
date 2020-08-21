using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Supabase.Auth
{
    public static class Helpers
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> MakeRequest(HttpMethod method, string url, Dictionary<string, string> reqParams = null, Dictionary<string, string> headers = null)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            builder.Port = -1;

            if (reqParams != null && method == HttpMethod.Get)
            {
                foreach (var param in reqParams)
                    query[param.Key] = param.Value;
            }

            builder.Query = query.ToString();

            var requestMessage = new HttpRequestMessage(method, builder.Uri);

            if (reqParams != null && method != HttpMethod.Get)
            {
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(reqParams), Encoding.UTF8, "application/json");
            }

            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            var response = await client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return content;
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(content);

                if (errorResponse.Code == "400")
                    throw new UserExistsException(errorResponse.Message);
                else
                    throw new Exception(response.ReasonPhrase);
            }
        }
    }

    public class UserExistsException : Exception
    {
        public UserExistsException(string message) : base(message) { }
    }
}

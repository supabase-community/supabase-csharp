using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Postgrest.Responses;

namespace Supabase.Postgrest
{
    public static class Helpers
    {
        public enum Operator
        {
            [MapTo("and")]
            And,
            [MapTo("or")]
            Or,
            [MapTo("eq")]
            Equals,
            [MapTo("gt")]
            GreaterThan,
            [MapTo("gte")]
            GreaterThanOrEqual,
            [MapTo("lt")]
            LessThan,
            [MapTo("lte")]
            LessThanOrEqual,
            [MapTo("neq")]
            NotEqual,
            [MapTo("like")]
            Like,
            [MapTo("ilike")]
            ILike,
            [MapTo("in")]
            In,
            [MapTo("is")]
            Is,
            [MapTo("fts")]
            FTS,
            [MapTo("plfts")]
            PLFTS,
            [MapTo("phfts")]
            PHFTS,
            [MapTo("wfts")]
            WFTS,
            [MapTo("cs")]
            Contains,
            [MapTo("cd")]
            ContainedIn,
            [MapTo("ov")]
            Overlap,
            [MapTo("sl")]
            StrictlyLeft,
            [MapTo("sr")]
            StrictlyRight,
            [MapTo("nxr")]
            NotRightOf,
            [MapTo("nxl")]
            NotLeftOf,
            [MapTo("adj")]
            Adjacent,
            [MapTo("not")]
            Not,
        }

        public enum Ordering
        {
            [MapTo("asc")]
            Ascending,
            [MapTo("desc")]
            Descending,
        }

        public enum NullPosition
        {
            [MapTo("nullsfirst")]
            First,
            [MapTo("nullslast")]
            Last
        }

        private static readonly HttpClient client = new HttpClient();

        public static async Task<ModeledResponse<T>> MakeRequest<T>(HttpMethod method, string url, Dictionary<string, string> reqParams = null, Dictionary<string, string> headers = null)
        {
            var baseResponse = await MakeRequest(method, url, reqParams, headers);
            return new ModeledResponse<T>(baseResponse);
        }

        public static async Task<BaseResponse> MakeRequest(HttpMethod method, string url, Dictionary<string, string> reqParams = null, Dictionary<string, string> headers = null)
        {
            try
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

                if (!response.IsSuccessStatusCode)
                {
                    var obj = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    obj.Content = content;
                    throw new RequestException(response, obj);
                }
                else
                {
                    return new BaseResponse { Content = content, ResponseMessage = response };
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw e;
            }
        }
    }

    public class RequestException : Exception
    {
        public HttpResponseMessage Response { get; private set; }
        public ErrorResponse Error { get; private set; }

        public RequestException(HttpResponseMessage response, ErrorResponse error) : base(error.Message)
        {
            Response = response;
            Error = error;
        }
    }
}

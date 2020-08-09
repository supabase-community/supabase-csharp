using System;
using System.Collections.Generic;
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
            try
            {
                var baseResponse = await MakeRequest(method, url, reqParams, headers);

                if (!string.IsNullOrWhiteSpace(baseResponse.Content))
                {
                    return new ModeledResponse<T>(baseResponse);
                }
                throw new Exception("Failed to Deserialize object");
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
            return default;
        }

        public static async Task<BaseResponse> MakeRequest(HttpMethod method, string url, Dictionary<string, string> reqParams = null, Dictionary<string, string> headers = null)
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

            if (reqParams != null && method == HttpMethod.Post)
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

            try
            {
                var response = await client.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var obj = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    obj.Content = content;
                    obj.ResponseMessage = response;

                    return obj;
                }
                else
                {
                    return new BaseResponse { Content = content, ResponseMessage = response };
                }
            }
            catch (Exception e)
            {
                return new ErrorResponse { Message = e.Message };
            }
        }

    }


}

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
            [ProcessAs("and")]
            And,
            [ProcessAs("or")]
            Or,
            [ProcessAs("eq")]
            Equals,
            [ProcessAs("gt")]
            GreaterThan,
            [ProcessAs("gte")]
            GreaterThanOrEqual,
            [ProcessAs("lt")]
            LessThan,
            [ProcessAs("lte")]
            LessThanOrEqual,
            [ProcessAs("neq")]
            NotEqual,
            [ProcessAs("like")]
            Like,
            [ProcessAs("ilike")]
            ILike,
            [ProcessAs("in")]
            In,
            [ProcessAs("is")]
            Is,
            [ProcessAs("fts")]
            FTS,
            [ProcessAs("plfts")]
            PLFTS,
            [ProcessAs("phfts")]
            PHFTS,
            [ProcessAs("wfts")]
            WFTS,
            [ProcessAs("cs")]
            Contains,
            [ProcessAs("cd")]
            ContainedIn,
            [ProcessAs("ov")]
            Overlap,
            [ProcessAs("sl")]
            StrictlyLeft,
            [ProcessAs("sr")]
            StrictlyRight,
            [ProcessAs("nxr")]
            NotRightOf,
            [ProcessAs("nxl")]
            NotLeftOf,
            [ProcessAs("adj")]
            Adjacent,
            [ProcessAs("not")]
            Not,
        }

        public enum Ordering
        {
            [ProcessAs("asc")]
            Ascending,
            [ProcessAs("desc")]
            Descending,
        }

        public enum NullPosition
        {
            [ProcessAs("nullsfirst")]
            First,
            [ProcessAs("nullslast")]
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

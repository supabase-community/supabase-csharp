using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Supabase.Functions.Client;

namespace Supabase
{
    public class SupabaseFunctions
    {
        private string _functionsUrl;
        private Dictionary<string, string> _headers = new Dictionary<string, string>();

        public SupabaseFunctions(string functionsUrl, Dictionary<string, string> headers)
        {
            _functionsUrl = functionsUrl.TrimEnd('/');
            _headers = headers;
        }

        public Task<string> Invoke(string functionName, Dictionary<string, object> body = null) => Functions.Client.Invoke($"{_functionsUrl}/{functionName}", options: new InvokeFunctionOptions
        {
            Headers = _headers,
            Body = body
        });

        public Task<T> Invoke<T>(string functionName, Dictionary<string, object> body = null) => Functions.Client.Invoke<T>($"{_functionsUrl}/{functionName}", options: new InvokeFunctionOptions
        {
            Headers = _headers,
            Body = body
        });

        public Task<HttpContent> RawInvoke(string functionName, Dictionary<string, object> body = null) => Functions.Client.RawInvoke($"{_functionsUrl}/{functionName}", options: new InvokeFunctionOptions
        {
            Headers = _headers,
            Body = body
        });
    }
}

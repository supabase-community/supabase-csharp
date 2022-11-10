using Supabase.Functions.Interfaces;
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
        private string functionsUrl;
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private IFunctionsClient client;

        public SupabaseFunctions(IFunctionsClient client, string functionsUrl, Dictionary<string, string> headers)
        {
            this.client = client;
            this.functionsUrl = functionsUrl.TrimEnd('/');
            this.headers = headers;
        }

        public Task<string> Invoke(string functionName, Dictionary<string, object>? body = null) => client.Invoke($"{functionsUrl}/{functionName}", options: new InvokeFunctionOptions
        {
            Headers = headers,
            Body = body ?? new Dictionary<string, object>()
        });

        public Task<T?> Invoke<T>(string functionName, Dictionary<string, object>? body = null) where T : class => client.Invoke<T>($"{functionsUrl}/{functionName}", options: new InvokeFunctionOptions
        {
            Headers = headers,
            Body = body ?? new Dictionary<string, object>()
        });

        public Task<HttpContent> RawInvoke(string functionName, Dictionary<string, object>? body = null) => client.RawInvoke($"{functionsUrl}/{functionName}", options: new InvokeFunctionOptions
        {
            Headers = headers,
            Body = body ?? new Dictionary<string, object>()
        });
    }
}

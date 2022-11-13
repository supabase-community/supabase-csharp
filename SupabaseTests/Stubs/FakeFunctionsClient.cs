using Supabase.Functions.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeFunctionsClient : IFunctionsClient
    {
        public Func<Dictionary<string, string>> GetHeaders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<string> Invoke(string url, string token = null, Supabase.Functions.Client.InvokeFunctionOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> Invoke<T>(string url, string token = null, Supabase.Functions.Client.InvokeFunctionOptions options = null) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<HttpContent> RawInvoke(string url, string token = null, Supabase.Functions.Client.InvokeFunctionOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}

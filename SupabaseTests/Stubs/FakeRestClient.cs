using Postgrest;
using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeRestClient : IPostgrestClient
    {
        public IPostgrestTableWithCache<T> Table<T>(IPostgrestCacheProvider cacheProvider) where T : BaseModel, new()
        {
            throw new NotImplementedException();
        }

        public string BaseUrl => throw new NotImplementedException();

        public ClientOptions Options => throw new NotImplementedException();

        public Func<Dictionary<string, string>> GetHeaders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AddRequestPreparedHandler(OnRequestPreparedEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void RemoveRequestPreparedHandler(OnRequestPreparedEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void ClearRequestPreparedHandlers()
        {
            throw new NotImplementedException();
        }

        public void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void ClearDebugHandlers()
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse> Rpc(string procedureName, object parameters)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public IPostgrestTable<T> Table<T>() where T : BaseModel, new()
        {
            throw new NotImplementedException();
        }
    }
}

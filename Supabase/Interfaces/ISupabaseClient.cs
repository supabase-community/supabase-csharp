using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Storage;
using Supabase.Storage.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    public interface ISupabaseClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>
        where TUser : User
        where TSession : Session
        where TSocket : IRealtimeSocket
        where TChannel : IRealtimeChannel
        where TBucket : Bucket
        where TFileObject : FileObject
    {
        IGotrueClient<TUser, TSession> Auth { get; set; }
        IFunctionsClient Functions { get; set; }
        IPostgrestClient Postgrest { get; set; }
        IRealtimeClient<TSocket, TChannel> Realtime { get; set; }
        IStorageClient<TBucket, TFileObject> Storage { get; set; }

        ISupabaseTable<TModel, TChannel> From<TModel>() where TModel : BaseModel, new();
        Task<ISupabaseClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>> InitializeAsync();
        Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters);
    }
}
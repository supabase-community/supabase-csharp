using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using Storage.Interfaces;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    public interface ISupabaseClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>
        where TUser: User
        where TSession: Session
        where TSocket: IRealtimeSocket
        where TChannel: IRealtimeChannel
        where TBucket: Bucket
        where TFileObject: FileObject
    {
        IGotrueClient<TUser, TSession> Auth { get; }
        IGotrueClient<TUser, TSession> AuthClient { get; set; }
        SupabaseFunctions Functions { get; }
        IFunctionsClient FunctionsClient { get; set; }
        IPostgrestClient Postgrest { get; }
        IPostgrestClient PostgrestClient { get; set; }
        IRealtimeClient<TSocket, TChannel> Realtime { get; }
        IRealtimeClient<TSocket, TChannel> RealtimeClient { get; set; }
        IStorageClient<TBucket, TFileObject> Storage { get; }
        IStorageClient<TBucket, TFileObject> StorageClient { get; set; }

        ISupabaseTable<TModel, TChannel> From<TModel>() where TModel : BaseModel, new();
        Task<ISupabaseClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>> InitializeAsync();
        Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters);
    }
}
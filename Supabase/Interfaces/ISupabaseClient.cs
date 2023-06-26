using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Realtime.Interfaces;
using Supabase.Storage;
using Supabase.Storage.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    /// <summary>
    /// Contract for what a SupabaseClient should implement
    /// </summary>
    /// <typeparam name="TUser">Model representing User</typeparam>
    /// <typeparam name="TSession">Model representing Session</typeparam>
    /// <typeparam name="TSocket">Class that conforms to <see cref="IRealtimeSocket"/></typeparam>
    /// <typeparam name="TChannel">Class that conforms to <see cref="IRealtimeChannel"/></typeparam>
    /// <typeparam name="TBucket">Model representing <see cref="Bucket"/></typeparam>
    /// <typeparam name="TFileObject">Model representing <see cref="FileObject"/></typeparam>
    public interface ISupabaseClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>
        where TUser : User
        where TSession : Session
        where TSocket : IRealtimeSocket
        where TChannel : IRealtimeChannel
        where TBucket : Bucket
        where TFileObject : FileObject
    {
        /// <summary>
        /// The Gotrue Auth Instance
        /// </summary>
        IGotrueClient<TUser, TSession> Auth { get; set; }

        /// <summary>
        /// Creates a Gotrue Admin Auth Client
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <returns></returns>
        IGotrueAdminClient<User> AdminAuth(string serviceKey);
        
        /// <summary>
        /// The Supabase Functions Client
        /// </summary>
        IFunctionsClient Functions { get; set; }
        
        /// <summary>
        /// The Postgrest Client
        /// </summary>
        IPostgrestClient Postgrest { get; set; }
        
        /// <summary>
        /// The Realtime Client
        /// </summary>
        IRealtimeClient<TSocket, TChannel> Realtime { get; set; }
        
        /// <summary>
        /// The Storage Client
        /// </summary>
        IStorageClient<TBucket, TFileObject> Storage { get; set; }

        /// <summary>
        /// Used for interacting with a Postgrest Table + Model. Provides helpers
        /// to be able to add realtime listeners and queries. 
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        ISupabaseTable<TModel, TChannel> From<TModel>() where TModel : BaseModel, new();
        
        
        /// <summary>
        /// Initializes a supabase client according to the provided <see cref="SupabaseOptions"/>.
        /// If option is enabled:
        /// - Will connect to realtime instance <see cref="SupabaseOptions.AutoConnectRealtime"/>
        /// - Will restore session using a <see cref="IGotrueSessionPersistence{TSession}"/> specified in <see cref="SupabaseOptions.SessionHandler"/>
        /// </summary>
        /// <returns></returns>
        Task<ISupabaseClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>> InitializeAsync();
        
        /// <summary>
        /// The RPC Client
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters);
    }
}
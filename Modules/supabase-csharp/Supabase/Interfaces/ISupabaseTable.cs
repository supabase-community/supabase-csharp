using System.Threading.Tasks;
using Supabase.Realtime.Interfaces;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace Supabase.Interfaces
{
    /// <summary>
    /// Contract representing a supabase wrapped postgrest <see cref="IPostgrestTable{TModel}"/>
    /// </summary>
    /// <typeparam name="TModel">Model that inherits from <see cref="BaseModel"/> that represents this Table</typeparam>
    /// <typeparam name="TChannel">Class that implements <see cref="IRealtimeChannel"/></typeparam>
    public interface ISupabaseTable<TModel, TChannel> : IPostgrestTable<TModel>
        where TModel : BaseModel, new()
        where TChannel : IRealtimeChannel
    {
        /// <summary>
        /// Add a realtime listener to this table.
        /// </summary>
        /// <param name="listenType"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task<TChannel> On(ListenType listenType, IRealtimeChannel.PostgresChangesHandler handler);
    }
}
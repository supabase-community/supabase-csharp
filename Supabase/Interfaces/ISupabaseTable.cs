using Postgrest.Interfaces;
using Postgrest.Models;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;
using System;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    /// <summary>
    /// Contract representing a supabase wrapped postgrest <see cref="IPostgrestTable{T}"/>
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
        /// <param name="eventType"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        Task<TChannel> On(Client.ChannelEventType eventType, Action<object, PostgresChangesEventArgs> action);
    }
}
using Postgrest.Interfaces;
using Postgrest.Models;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;
using System;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    public interface ISupabaseTable<TModel, TChannel> : IPostgrestTable<TModel>
        where TModel : BaseModel, new()
        where TChannel : IRealtimeChannel
    {
        Task<TChannel> On(Client.ChannelEventType e, Action<object, PostgresChangesEventArgs> action);
    }
}
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;

namespace Supabase.Realtime;

public class Binding
{
    public PostgresChangesOptions? Options { get; set; }

    public IRealtimeChannel.PostgresChangesHandler? Handler { get; set; }
    
    public PostgresChangesOptions.ListenType? ListenType { get; set; }

    public int? Id { get; set; }
}
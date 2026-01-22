using Newtonsoft.Json;
using Supabase.Realtime.Models;
using Supabase.Realtime.Socket;
using System.Collections.Generic;

namespace Supabase.Realtime.Presence.Responses;

/// <inheritdoc />
public class PresenceStateSocketResponse<TPresence> : SocketResponse<Dictionary<string, PresenceStatePayload<TPresence>>> 
    where TPresence : BasePresence
{
    /// <inheritdoc />
    public PresenceStateSocketResponse(JsonSerializerSettings serializerSettings) : base(serializerSettings) { }
}

/// <summary>
/// A presence state payload response
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class PresenceStatePayload<TPresence> where TPresence : BasePresence
{
    /// <summary>
    /// The metas containing joins and leaves
    /// </summary>
    [JsonProperty("metas")]
    public List<TPresence>? Metas { get; set; }
}
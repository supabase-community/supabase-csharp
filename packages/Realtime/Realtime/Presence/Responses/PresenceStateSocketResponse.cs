using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase.Realtime.Models;
using Supabase.Realtime.Socket;

namespace Supabase.Realtime.Presence.Responses;

/// <inheritdoc />
public class PresenceStateSocketResponse<TPresence> : SocketResponse<Dictionary<string, PresenceStatePayload<TPresence>>>
    where TPresence : BasePresence
{
    /// <summary>Parameterless constructor used by System.Text.Json when deserializing.</summary>
    public PresenceStateSocketResponse() { }

    /// <inheritdoc />
    public PresenceStateSocketResponse(JsonSerializerOptions serializerSettings) : base(serializerSettings) { }
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
    [JsonPropertyName("metas")]
    public List<TPresence>? Metas { get; set; }
}

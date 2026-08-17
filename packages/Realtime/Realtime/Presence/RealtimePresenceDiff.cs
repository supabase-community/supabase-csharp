using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase.Realtime.Models;
using Supabase.Realtime.Socket;

namespace Supabase.Realtime.Presence;

/// <summary>
/// Represents a presence_diff response
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class RealtimePresenceDiff<TPresence> : SocketResponse<PresenceDiffPayload<TPresence>> where TPresence : BasePresence
{
    /// <summary>Parameterless constructor used by System.Text.Json when deserializing.</summary>
    public RealtimePresenceDiff() { }

    /// <inheritdoc />
    public RealtimePresenceDiff(JsonSerializerOptions serializerSettings) : base(serializerSettings)
    { }
}

/// <summary>
/// a Presence Diff payload
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class PresenceDiffPayload<TPresence> where TPresence : BasePresence
{
    /// <summary>
    /// The joining presences.
    /// </summary>
    [JsonPropertyName("joins")]
    public Dictionary<string, PresenceDiffPayloadMeta<TPresence>>? Joins { get; set; }

    /// <summary>
    /// The leaving presences.
    /// </summary>
    [JsonPropertyName("leaves")]
    public Dictionary<string, PresenceDiffPayloadMeta<TPresence>>? Leaves { get; set; }
}

/// <summary>
/// A presence diff payload
/// </summary>
/// <typeparam name="TPresence"></typeparam>
public class PresenceDiffPayloadMeta<TPresence> where TPresence : BasePresence
{
    /// <summary>
    /// The metas containing current presences
    /// </summary>
    [JsonPropertyName("metas")]
    public List<TPresence>? Metas { get; set; }
}

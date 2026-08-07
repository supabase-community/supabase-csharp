using Newtonsoft.Json;
using Supabase.Realtime.Channel;

namespace Supabase.Realtime.Socket;

/// <summary>
/// Representation of a Socket Request, used by <see cref="Push"/>
/// </summary>
public class SocketRequest
{
    /// <summary>
    /// The type
    /// </summary>
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    /// <summary>
    /// The topic being sent to
    /// </summary>
    [JsonProperty("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// The Event name
    /// </summary>
    [JsonProperty("event")]
    public string? Event { get; set; }

    /// <summary>
    /// The json serializable payload
    /// </summary>
    [JsonProperty("payload")]
    public object? Payload { get; set; }

    /// <summary>
    /// The unique ref for this request.
    /// </summary>
    [JsonProperty("ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// The join ref (if applicable)
    /// </summary>
    [JsonProperty("join_ref", NullValueHandling = NullValueHandling.Ignore)]
    public string? JoinRef { get; set; }
}
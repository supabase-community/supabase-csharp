using System.Text.Json.Serialization;
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
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// The topic being sent to
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// The Event name
    /// </summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>
    /// The json serializable payload
    /// </summary>
    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    /// <summary>
    /// The unique ref for this request.
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// The join ref (if applicable)
    /// </summary>
    [JsonPropertyName("join_ref")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JoinRef { get; set; }
}

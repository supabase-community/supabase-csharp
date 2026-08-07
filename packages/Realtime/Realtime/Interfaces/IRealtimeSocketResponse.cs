using Supabase.Realtime.Socket;

namespace Supabase.Realtime.Interfaces;

/// <summary>
/// Contract for a socket response
/// </summary>
public interface IRealtimeSocketResponse
{
    /// <summary>
    /// The raw event name
    /// </summary>
    string? _event { get; set; }
        
    /// <summary>
    /// The parsed event type
    /// </summary>
    Constants.EventType Event { get; }
        
    /// <summary>
    /// The opinionated payload matching a <see cref="SocketResponsePayload"/>
    /// </summary>
    SocketResponsePayload? Payload { get; set; }
        
    /// <summary>
    /// The unique id of this response
    /// </summary>
    string? Ref { get; set; }
        
    /// <summary>
    /// The topic.
    /// </summary>
    string? Topic { get; set; }
}
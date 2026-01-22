using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;
using Supabase.Realtime.Socket;

namespace Supabase.Realtime;

/// <summary>
/// Options used when initializing a <see cref="Client"/>
/// </summary>
public class ClientOptions
{
    /// <summary>
    /// The function to encode outgoing messages. Defaults to JSON
    /// </summary>
    public Action<object, Action<string>>? Encode { get; set; }

    /// <summary>
    /// The function to decode incoming messages.
    /// </summary>
    public Action<string, Action<SocketResponse?>>? Decode { get; set; }

    /// <summary>
    /// The Websocket Transport, for example WebSocket.
    /// </summary>
    public string Transport { get; set; } = Constants.TransportWebsocket;

    /// <summary>
    /// The default timeout in milliseconds to trigger push timeouts.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(Constants.DefaultTimeout);

    /// <summary>
    /// @todo Presently unused: Limit the number of events that can be sent per second.
    /// </summary>
    public int EventsPerSecond { get; set; } = 10;

    /// <summary>
    /// The interval to send a heartbeat message
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The interval to reconnect
    /// </summary>
    public Func<int, TimeSpan> ReconnectAfterInterval { get; set; } = (tries) =>
    {
        var intervals = new[] { 30, 45, 60, 120 };
        return TimeSpan.FromSeconds(tries < intervals.Length ? intervals[tries] : intervals[intervals.Length - 1]);
    };

    /// <summary>
    /// Request headers to be appended to the connection string.
    /// </summary>
    public readonly Dictionary<string, string> Headers = new();

    /// <summary>
    /// The optional params to pass when connecting
    /// </summary>
    public SocketOptionsParameters Parameters = new();

    /// <summary>
    /// Datetime Style for JSON Deserialization of Models
    /// </summary>
    public readonly DateTimeStyles DateTimeStyles = DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Datetime format for JSON Deserialization of Models (Postgrest style)
    /// </summary>
    public string DateTimeFormat { get; set; } = @"yyyy'-'MM'-'dd' 'HH':'mm':'ss.FFFFFFK";
}
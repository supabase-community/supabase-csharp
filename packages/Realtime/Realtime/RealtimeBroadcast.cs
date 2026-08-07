using Newtonsoft.Json;
using Supabase.Realtime.Broadcast;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Supabase.Realtime.Socket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime;

/// <summary>
/// Represents a realtime broadcast client.
/// 
/// Broadcast follows the publish-subscribe pattern where a client publishes messages to a channel with a unique identifier.
/// Other clients can elect to receive the message in real-time by subscribing to the channel with the same unique identifier. If these clients are online and subscribed then they will receive the message.
///
/// Broadcast works by connecting your client to the nearest Realtime server, which will communicate with other servers to relay messages to other clients.
/// A common use-case is sharing a user's cursor position with other clients in an online game.
/// </summary>
/// <typeparam name="TBroadcastModel">A model representing expected payload.</typeparam>
public class RealtimeBroadcast<TBroadcastModel> : IRealtimeBroadcast where TBroadcastModel : BaseBroadcast
{
    private readonly RealtimeChannel _channel;
    private readonly JsonSerializerSettings _serializerSettings;

    private SocketResponse? _lastSocketResponse;

    private readonly List<IRealtimeBroadcast.BroadcastEventHandler> _broadcastEventHandlers = new();

    /// <summary>
    /// The last received broadcast.
    /// </summary>
    public TBroadcastModel? Current()
    {
        if (_lastSocketResponse == null) return null;

        var obj = JsonConvert.DeserializeObject<SocketResponse<TBroadcastModel>>(_lastSocketResponse.Json!,
            _serializerSettings);

        if (obj == null || obj.Payload == null) return null;

        return obj.Payload;
    }

    /// <summary>
    /// Initializes a realtime broadcast helper class.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="options"></param>
    /// <param name="serializerSettings"></param>
    public RealtimeBroadcast(RealtimeChannel channel, BroadcastOptions options,
        JsonSerializerSettings serializerSettings)
    {
        _channel = channel;
        _serializerSettings = serializerSettings;
    }

    /// <summary>
    /// Adds a broadcast event listener.
    /// </summary>
    /// <param name="broadcastEventHandler"></param>
    public void AddBroadcastEventHandler(IRealtimeBroadcast.BroadcastEventHandler broadcastEventHandler)
    {
        if (!_broadcastEventHandlers.Contains(broadcastEventHandler))
            _broadcastEventHandlers.Add(broadcastEventHandler);
    }

    /// <summary>
    /// Removes a broadcast event listener.
    /// </summary>
    /// <param name="broadcastEventHandler"></param>
    public void RemoveBroadcastEventHandler(IRealtimeBroadcast.BroadcastEventHandler broadcastEventHandler)
    {
        if (_broadcastEventHandlers.Contains(broadcastEventHandler))
            _broadcastEventHandlers.Remove(broadcastEventHandler);
    }

    /// <summary>
    /// Clears all broadcast event listeners
    /// </summary>
    public void ClearBroadcastEventHandlers() =>
        _broadcastEventHandlers.Clear();

    private void NotifyBroadcastEventHandlers()
    {
        foreach (var handler in _broadcastEventHandlers.ToArray())
            handler.Invoke(this, Current());
    }

    /// <summary>
    /// Called by <see cref="RealtimeChannel"/> when a broadcast event is received, then parsed/typed here.
    /// </summary>
    /// <param name="response"></param>
    /// <exception cref="ArgumentException"></exception>
    public void TriggerReceived(SocketResponse response)
    {
        if (response == null || response.Json == null)
            throw new ArgumentException(
                $"Expected parsable JSON response, instead received: `{JsonConvert.SerializeObject(response)}`");

        _lastSocketResponse = response;
        NotifyBroadcastEventHandlers();
    }

    /// <summary>
    /// Broadcasts an arbitrary payload
    /// </summary>
    /// <param name="broadcastEventName"></param>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    public Task<bool> Send(string? broadcastEventName, object payload, int timeoutMs = 10000)
    {
        if (payload is BaseBroadcast baseBroadcast && string.IsNullOrEmpty(baseBroadcast.Event))
            baseBroadcast.Event = broadcastEventName;

        return _channel.Send(ChannelEventName.Broadcast, broadcastEventName, payload, timeoutMs);
    }
}
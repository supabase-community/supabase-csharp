using Newtonsoft.Json;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Supabase.Realtime.Presence;
using Supabase.Realtime.Presence.Responses;
using Supabase.Realtime.Socket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase.Realtime.Channel;
using Supabase.Realtime.Exceptions;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime;

/// <summary>
/// Represents a realtime presence client.
/// 
/// When a client subscribes to a channel, it will immediately receive the channel's latest state in a single message.
/// Clients are free to come-and-go as they please, and as long as they are all subscribed to the same channel then they will all have the same Presence state as each other.
/// If a client is suddenly disconnected (for example, they go offline), their state will be automatically removed from the shared state.
/// </summary>
/// <typeparam name="TPresenceModel">A model representing expected payload.</typeparam>
public class RealtimePresence<TPresenceModel> : IRealtimePresence where TPresenceModel : BasePresence
{
    /// <summary>
    /// The Last State of this Presence instance.
    /// </summary>
    public Dictionary<string, List<TPresenceModel>> LastState { get; private set; } =
        new();

    /// <summary>
    /// The Current State of this Presence instance.
    /// </summary>
    public Dictionary<string, List<TPresenceModel>> CurrentState { get; } = new();

    private PresenceOptions _options;
    private SocketResponse? _currentResponse;
    private readonly RealtimeChannel _channel;
    private readonly JsonSerializerSettings _serializerSettings;

    private readonly Dictionary<IRealtimePresence.EventType, List<IRealtimePresence.PresenceEventHandler>>
        _presenceEventListeners = new();

    /// <summary>
    /// Initializes a realtime presence helper class.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="options"></param>
    /// <param name="serializerSettings"></param>
    public RealtimePresence(RealtimeChannel channel, PresenceOptions options,
        JsonSerializerSettings serializerSettings)
    {
        _channel = channel;
        _options = options;
        _serializerSettings = serializerSettings;
    }

    /// <summary>
    /// Add presence event handler for a given event type.
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public void AddPresenceEventHandler(IRealtimePresence.EventType eventType,
        IRealtimePresence.PresenceEventHandler handler)
    {
        if (!_presenceEventListeners.ContainsKey(eventType))
            _presenceEventListeners[eventType] = new List<IRealtimePresence.PresenceEventHandler>();

        if (!_presenceEventListeners[eventType].Contains(handler))
            _presenceEventListeners[eventType].Add(handler);
    }

    /// <summary>
    /// Remove an event handler
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public void RemovePresenceEventHandlers(IRealtimePresence.EventType eventType,
        IRealtimePresence.PresenceEventHandler handler)
    {
        if (_presenceEventListeners.ContainsKey(eventType) &&
            _presenceEventListeners[eventType].Contains(handler))
            _presenceEventListeners[eventType].Remove(handler);
    }

    /// <summary>
    /// Clears all event handlers for a given type (if specified) or clears all handlers.
    /// </summary>
    /// <param name="eventType"></param>
    public void ClearPresenceEventHandlers(IRealtimePresence.EventType? eventType = null)
    {
        if (eventType != null && _presenceEventListeners.TryGetValue(eventType.Value, out var list))
            list.Clear();
        else
            _presenceEventListeners.Clear();
    }

    /// <summary>
    /// Notifies listeners of state changes
    /// </summary>
    /// <param name="eventType"></param>
    private void NotifyPresenceEventHandlers(IRealtimePresence.EventType eventType)
    {
        if (!_presenceEventListeners.ContainsKey(eventType)) return;

        foreach (var handler in _presenceEventListeners[eventType].ToArray())
            handler.Invoke(this, eventType);
    }

    /// <summary>
    /// Called in two cases:
    ///		- By `RealtimeChannel` when it receives a `presence_state` initializing message.
    ///		- By `RealtimeChannel` When a diff has been received and a new response is saved.
    /// </summary>
    /// <param name="response"></param>
    public void TriggerSync(SocketResponse response)
    {
        _currentResponse = response;
        SetState();

        NotifyPresenceEventHandlers(IRealtimePresence.EventType.Sync);
    }

    /// <summary>
    /// Triggers a diff comparison and emits events accordingly.
    /// </summary>
    /// <param name="response"></param>
    /// <exception cref="ArgumentException"></exception>
    public void TriggerDiff(SocketResponse response)
    {
        if (response == null || response.Json == null)
            throw new ArgumentException(
                $"Expected parsable JSON response, instead received: `{JsonConvert.SerializeObject(response)}`");

        var obj = JsonConvert.DeserializeObject<RealtimePresenceDiff<TPresenceModel>>(response.Json,
            _serializerSettings);

        if (obj?.Payload == null) return;

        TriggerSync(response);

        if (obj.Payload.Joins!.Count > 0)
            NotifyPresenceEventHandlers(IRealtimePresence.EventType.Join);

        if (obj.Payload.Leaves!.Count > 0)
            NotifyPresenceEventHandlers(IRealtimePresence.EventType.Leave);
    }

    /// <summary>
    /// "Tracks" an event, used with <see cref="Presence"/>.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    public Task<Push> Track(object? payload, int timeoutMs = DefaultTimeout)
    {
        var eventName = Core.Helpers.GetMappedToAttr(ChannelEventName.Presence).Mapping;
        var push = new Push(_channel.Socket, _channel, eventName, "track",
            new Dictionary<string, object?> { { "event", "track" }, { "payload", payload } }, timeoutMs);

        var tcs = new TaskCompletionSource<Push>();

        void Handler(IRealtimePush<RealtimeChannel, SocketResponse> chanel, SocketResponse response)
        {
            tcs.TrySetResult(push);
        }

        push.AddMessageReceivedHandler(Handler);

        push.OnTimeout += (sender, args) =>
        {
            if (sender is Push p)
                tcs.SetException(new RealtimeException($"Failed to send push [{p.Ref}])")
                    { Reason = FailureHint.Reason.PushTimeout });
        };

        _channel.Enqueue(push);

        return tcs.Task;
    }

    /// <summary>
    /// Untracks an event.
    /// </summary>
    public Task<Push> Untrack()
    {
        var eventName = Core.Helpers.GetMappedToAttr(ChannelEventName.Presence).Mapping;
        var push = new Push(_channel.Socket, _channel, eventName, "untrack",
            new Dictionary<string, object?> { { "event", "untrack" } });

        var tcs = new TaskCompletionSource<Push>();

        void Handler(IRealtimePush<RealtimeChannel, SocketResponse> chanel, SocketResponse response)
        {
            tcs.TrySetResult(push);
        }

        push.AddMessageReceivedHandler(Handler);

        push.OnTimeout += (sender, args) =>
        {
            if (sender is Push p)
                tcs.TrySetException(new RealtimeException($"Failed to send push [{p.Ref}])")
                    { Reason = FailureHint.Reason.PushTimeout });
        };

        _channel.Enqueue(push);
        return tcs.Task;
    }

    /// <summary>
    /// Sets the internal Presence State from the <see cref="_currentResponse"/>
    /// </summary>
    private void SetState()
    {
        LastState = new Dictionary<string, List<TPresenceModel>>(CurrentState);

        if (_currentResponse?.Json == null) return;

        // Is a diff response?
        if (_currentResponse.Payload!.Joins != null || _currentResponse.Payload!.Leaves != null)
        {
            var state = JsonConvert.DeserializeObject<RealtimePresenceDiff<TPresenceModel>>(_currentResponse.Json,
                _serializerSettings)!;

            if (state?.Payload == null) return;

            // Remove any result that has "left"
            foreach (var item in state.Payload.Leaves!)
                CurrentState.Remove(item.Key);

            // Add any results that have come in.
            foreach (var item in state.Payload.Joins!)
                CurrentState[item.Key] = item.Value.Metas!;
        }
        else
        {
            // It's a presence_state init response
            var state =
                JsonConvert.DeserializeObject<PresenceStateSocketResponse<TPresenceModel>>(_currentResponse.Json,
                    _serializerSettings)!;

            if (state?.Payload == null) return;

            foreach (var item in state.Payload)
                CurrentState[item.Key] = item.Value.Metas!;
        }
    }
}
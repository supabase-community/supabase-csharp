using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Supabase.Realtime.Broadcast;
using Supabase.Realtime.Channel;
using Supabase.Realtime.Exceptions;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Supabase.Realtime.PostgresChanges;
using Supabase.Realtime.Presence;
using Supabase.Realtime.Socket;
using Supabase.Realtime.Socket.Responses;
using static Supabase.Realtime.Constants;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;
using static Supabase.Realtime.Interfaces.IRealtimeChannel;
using Timer = System.Timers.Timer;

// ReSharper disable InvalidXmlDocComment

[assembly: InternalsVisibleTo("RealtimeTests")]

namespace Supabase.Realtime;

/// <summary>
/// Class representation of a channel subscription
/// </summary>
public class RealtimeChannel : IRealtimeChannel
{
    /// <summary>
    /// As to whether this Channel is Closed
    /// </summary>
    public bool IsClosed => State == ChannelState.Closed;

    /// <summary>
    /// As to if this Channel has Errored
    /// </summary>
    public bool IsErrored => State == ChannelState.Errored;

    /// <summary>
    /// As to if this Channel is currently Joined
    /// </summary>
    public bool IsJoined => State == ChannelState.Joined;

    /// <summary>
    /// As to if this Channel is currently Joining
    /// </summary>
    public bool IsJoining => State == ChannelState.Joining;

    /// <summary>
    /// As to if this channel is currently leaving
    /// </summary>
    public bool IsLeaving => State == ChannelState.Leaving;

    /// <summary>
    /// The channel's topic (identifier)
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// The Channel's current state.
    /// </summary>
    public ChannelState State { get; private set; } = ChannelState.Closed;

    /// <summary>
    /// Options passed to this channel instance.
    /// </summary>
    public ChannelOptions Options { get; }

    /// <summary>
    /// The saved Broadcast Options, set in <see cref="Register{TBroadcastResponse}(bool, bool)"/>
    /// </summary>
    public BroadcastOptions? BroadcastOptions { get; private set; } = new();

    /// <summary>
    /// The saved Presence Options, set in <see cref="Register{TPresenceResponse}(string)"/>
    /// </summary>
    public PresenceOptions? PresenceOptions { get; private set; } = new(string.Empty);

    /// <summary>
    /// The saved Postgres Changes Options, set in <see cref="Register(PostgresChanges.PostgresChangesOptions)"/>
    /// </summary>
    public List<PostgresChangesOptions> PostgresChangesOptions { get; } = new();

    /// <summary>
    /// Flag stating whether a channel has been joined once or not.
    /// </summary>
    public bool HasJoinedOnce { get; private set; }

    /// <summary>
    /// Flag stating if a channel is currently subscribed.
    /// </summary>
    public bool IsSubscribed;

    /// <summary>
    /// Returns the <see cref="IRealtimeBroadcast"/> instance.
    /// </summary>
    /// <returns></returns>
    public IRealtimeBroadcast? Broadcast() => _broadcast;

    /// <summary>
    /// Returns a typed <see cref="RealtimeBroadcast{TBroadcastModel}" /> instance.
    /// </summary>
    /// <typeparam name="TBroadcastModel"></typeparam>
    /// <returns></returns>
    public RealtimeBroadcast<TBroadcastModel>? Broadcast<TBroadcastModel>() where TBroadcastModel : BaseBroadcast =>
        _broadcast != null ? (RealtimeBroadcast<TBroadcastModel>)_broadcast : default;

    /// <summary>
    /// Returns the <see cref="IRealtimePresence"/> instance.
    /// </summary>
    /// <returns></returns>
    public IRealtimePresence? Presence() => _presence;

    /// <summary>
    /// Returns a typed <see cref="RealtimePresence{T}"/> instance.
    /// </summary>
    /// <typeparam name="TPresenceModel">Model representing a Presence payload</typeparam>
    /// <returns></returns>
    public RealtimePresence<TPresenceModel>? Presence<TPresenceModel>() where TPresenceModel : BasePresence =>
        _presence != null ? (RealtimePresence<TPresenceModel>)_presence : default;

    /// <summary>
    /// The initial request to join a channel (repeated on channel disconnect)
    /// </summary>
    internal Push? JoinPush;

    internal Push? LastPush;

    // Event handlers that pass events to typed instances for broadcast and presence.
    internal delegate void BroadcastEventHandler(IRealtimeChannel sender, SocketResponse response);

    internal delegate void PresenceDiffHandler(IRealtimeChannel sender, SocketResponse response);

    internal delegate void PresenceSyncHandler(IRealtimeChannel sender, SocketResponse response);

    internal BroadcastEventHandler? BroadcastHandler;
    internal PresenceDiffHandler? PresenceDiff;
    internal PresenceSyncHandler? PresenceSync;

    /// <summary>
    /// Buffer of Pushes held because of Socket availability
    /// </summary>
    private readonly List<Push> _buffer = new();

    internal readonly IRealtimeSocket Socket;
    private IRealtimePresence? _presence;
    private IRealtimeBroadcast? _broadcast;
    private RealtimeException? _exception;

    private readonly List<StateChangedHandler> _stateChangedHandlers = new();
    private readonly List<MessageReceivedHandler> _messageReceivedHandlers = new();
    private readonly List<ErrorEventHandler> _errorEventHandlers = new();

    private bool CanPush => IsJoined && Socket.IsConnected;
    private bool _hasJoinedOnce;
    private readonly Timer _rejoinTimer;
    private bool _isRejoining;

    private List<Binding> _bindings = [];

    /// <summary>
    /// Initializes a Channel - must call `Subscribe()` to receive events.
    /// </summary>
    public RealtimeChannel(IRealtimeSocket socket, string channelName, ChannelOptions options)
    {
        Topic = channelName;
        Options = options;
        Options.Parameters ??= new Dictionary<string, string>();

        Socket = socket;
        Socket.AddStateChangedHandler(HandleSocketStateChanged);

        _rejoinTimer = new Timer(options.ClientOptions.Timeout.TotalMilliseconds);
        _rejoinTimer.Elapsed += HandleRejoinTimerElapsed;
        _rejoinTimer.AutoReset = true;
    }

    /// <summary>
    /// Handles socket state changes, specifically when a socket reconnects this channel (if previously subscribed)
    /// should also rejoin.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="state"></param>
    private void HandleSocketStateChanged(IRealtimeSocket _, SocketState state)
    {
        if (state != SocketState.Reconnect || !IsSubscribed) return;

        Rejoin();
    }

    /// <summary>
    /// Registers a <see cref="RealtimeBroadcast{TBroadcastModel}"/> instance - allowing broadcast responses to be parsed.
    /// </summary>
    /// <typeparam name="TBroadcastResponse"></typeparam>
    /// <param name="broadcastSelf">enables client to receive message it has broadcast</param>
    /// <param name="broadcastAck">instructs server to acknowledge that broadcast message was received</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public RealtimeBroadcast<TBroadcastResponse> Register<TBroadcastResponse>(bool broadcastSelf = false,
        bool broadcastAck = false) where TBroadcastResponse : BaseBroadcast
    {
        if (_broadcast != null)
            throw new InvalidOperationException(
                "Register can only be called with broadcast options for a channel once.");

        BroadcastOptions = new BroadcastOptions(broadcastSelf, broadcastAck);

        var instance =
            new RealtimeBroadcast<TBroadcastResponse>(this, BroadcastOptions, Options.SerializerSettings);
        _broadcast = instance;

        BroadcastHandler = (_, response) => _broadcast.TriggerReceived(response);

        return instance;
    }

    /// <summary>
    /// Registers a <see cref="RealtimePresence{TPresenceResponse}"/> instance - allowing presence responses to be parsed and state to be tracked.
    /// </summary>
    /// <typeparam name="TPresenceResponse">The model representing a presence payload.</typeparam>
    /// <param name="presenceKey">used to track presence payload across clients</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown if called multiple times.</exception>
    public RealtimePresence<TPresenceResponse> Register<TPresenceResponse>(string presenceKey)
        where TPresenceResponse : BasePresence
    {
        if (_presence != null)
            throw new InvalidOperationException(
                "Register can only be called with presence options for a channel once.");

        PresenceOptions = new PresenceOptions(presenceKey);
        var instance = new RealtimePresence<TPresenceResponse>(this, PresenceOptions, Options.SerializerSettings);
        _presence = instance;

        PresenceSync = (_, response) => _presence.TriggerSync(response);
        PresenceDiff = (_, response) => _presence.TriggerDiff(response);

        return instance;
    }

    /// <summary>
    /// Registers a state changed listener relative to this channel. Called when channel state changes.
    /// </summary>
    /// <param name="stateChangedHandler"></param>
    public void AddStateChangedHandler(StateChangedHandler stateChangedHandler)
    {
        if (!_stateChangedHandlers.Contains(stateChangedHandler))
            _stateChangedHandlers.Add(stateChangedHandler);
    }

    /// <summary>
    /// Removes a channel state changed listener
    /// </summary>
    /// <param name="stateChangedHandler"></param>
    public void RemoveStateChangedHandler(StateChangedHandler stateChangedHandler)
    {
        if (_stateChangedHandlers.Contains(stateChangedHandler))
            _stateChangedHandlers.Remove(stateChangedHandler);
    }

    /// <summary>
    /// Clears all channel state changed listeners
    /// </summary>
    public void ClearStateChangedHandlers() =>
        _stateChangedHandlers.Clear();

    /// <summary>
    /// Notifies registered listeners that a channel state has changed.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="shouldRejoin"></param>
    private void NotifyStateChanged(ChannelState state, bool shouldRejoin = true)
    {
        State = state;

        _isRejoining = shouldRejoin;
        if (shouldRejoin)
            _rejoinTimer.Start();
        else
            _rejoinTimer.Stop();

        foreach (var handler in _stateChangedHandlers.ToArray())
            handler.Invoke(this, state);
    }

    /// <summary>
    /// Registers a message received listener, called when a socket message is received for this channel.
    /// </summary>
    /// <param name="messageReceivedHandler"></param>
    public void AddMessageReceivedHandler(MessageReceivedHandler messageReceivedHandler)
    {
        if (!_messageReceivedHandlers.Contains(messageReceivedHandler))
            _messageReceivedHandlers.Add(messageReceivedHandler);
    }

    /// <summary>
    /// Removes a message received listener.
    /// </summary>
    /// <param name="messageReceivedHandler"></param>
    public void RemoveMessageReceivedHandler(MessageReceivedHandler messageReceivedHandler)
    {
        if (_messageReceivedHandlers.Contains(messageReceivedHandler))
            _messageReceivedHandlers.Remove(messageReceivedHandler);
    }

    /// <summary>
    /// Clears message received listeners.
    /// </summary>
    public void ClearMessageReceivedHandlers() =>
        _messageReceivedHandlers.Clear();

    /// <summary>
    /// Notifies registered listeners that a channel message has been received.
    /// </summary>
    /// <param name="message"></param>
    private void NotifyMessageReceived(SocketResponse message)
    {
        foreach (var handler in _messageReceivedHandlers.ToArray())
            handler.Invoke(this, message);
    }

    /// <summary>
    /// Add a postgres changes listener. Should be paired with <see cref="Register"/>.
    /// </summary>
    /// <param name="listenType">The type of event this callback should process.</param>
    /// <param name="postgresChangeHandler"></param>
    public void AddPostgresChangeHandler(ListenType listenType, PostgresChangesHandler postgresChangeHandler)
    {
        BindPostgresChangesHandler(listenType, postgresChangeHandler);
    }

    /// <summary>
    /// Removes a postgres changes listener.
    /// </summary>
    /// <param name="listenType">The type of event this callback was registered to process.</param>
    /// <param name="postgresChangeHandler"></param>
    public void RemovePostgresChangeHandler(ListenType listenType, PostgresChangesHandler postgresChangeHandler)
    {
        RemovePostgresChangesFromBinding(listenType, postgresChangeHandler);
    }

    /// <summary>
    /// Clears all postgres changes listeners.
    /// </summary>
    public void ClearPostgresChangeHandlers()
    {
        _bindings.Clear();
    }

    /// <summary>
    /// Adds an error event handler.
    /// </summary>
    /// <param name="handler"></param>
    public void AddErrorHandler(ErrorEventHandler handler)
    {
        if (!_errorEventHandlers.Contains(handler))
            _errorEventHandlers.Add(handler);
    }

    /// <summary>
    /// Removes an error event handler
    /// </summary>
    /// <param name="handler"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveErrorHandler(ErrorEventHandler handler)
    {
        if (_errorEventHandlers.Contains(handler))
            _errorEventHandlers.Remove(handler);
    }

    /// <summary>
    /// Clears Error Event Handlers
    /// </summary>
    public void ClearErrorHandlers() =>
        _errorEventHandlers.Clear();

    private void NotifyErrorOccurred(RealtimeException exception)
    {
        _exception = exception;

        NotifyStateChanged(ChannelState.Errored);

        foreach (var handler in _errorEventHandlers)
            handler.Invoke(this, exception);
    }

    /// <summary>
    /// Notifies listeners of a postgres change message being received.
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="response"></param>
    private void NotifyPostgresChanges(EventType eventType, PostgresChangesResponse response)
    {
        var listenType = eventType switch
        {
            EventType.Insert => ListenType.Inserts,
            EventType.Delete => ListenType.Deletes,
            EventType.Update => ListenType.Updates,
            _ => ListenType.All
        };

        InvokeProperlyHandlerFromBind(listenType, response);
    }

    /// <summary>
    /// Registers postgres_changes options, can be called multiple times.
    ///
    /// Should be paired with <see cref="AddPostgresChangeHandler"/>
    /// </summary>
    /// <param name="postgresChangesOptions"></param>
    /// <returns></returns>
    public IRealtimeChannel Register(PostgresChangesOptions postgresChangesOptions)
    {
        PostgresChangesOptions.Add(postgresChangesOptions);
        
        BindPostgresChangesOptions(postgresChangesOptions);
        return this;
    }

    /// <summary>
    /// Subscribes to the channel given supplied Options/params.
    /// </summary>
    /// <param name="timeoutMs"></param>
    public Task<IRealtimeChannel> Subscribe(int timeoutMs = DefaultTimeout)
    {
        var tsc = new TaskCompletionSource<IRealtimeChannel>();

        if (IsSubscribed)
            return Task.FromResult(this as IRealtimeChannel);

        JoinPush = GenerateJoinPush();
        StateChangedHandler? channelCallback = null;
        EventHandler? joinPushTimeoutCallback = null;

        channelCallback = (sender, state) =>
        {
            switch (state)
            {
                // Success!
                case ChannelState.Joined:
                    HasJoinedOnce = true;
                    IsSubscribed = true;

                    sender.RemoveStateChangedHandler(channelCallback!);
                    JoinPush.OnTimeout -= joinPushTimeoutCallback;

                    // Clear buffer
                    foreach (var item in _buffer)
                        item.Send();
                    _buffer.Clear();

                    tsc.TrySetResult(this);
                    break;
                // Failure
                case ChannelState.Closed:
                case ChannelState.Errored:
                    sender.RemoveStateChangedHandler(channelCallback!);
                    JoinPush.OnTimeout -= joinPushTimeoutCallback;
                    tsc.TrySetException(_exception);
                    break;
            }
        };

        // Throw an exception if there is a problem receiving a join response
        joinPushTimeoutCallback = (_, _) =>
        {
            RemoveStateChangedHandler(channelCallback);
            JoinPush.OnTimeout -= joinPushTimeoutCallback;

            NotifyErrorOccurred(new RealtimeException("Push Timeout")
            {
                Reason = FailureHint.Reason.PushTimeout
            });
        };

        AddStateChangedHandler(channelCallback);

        // Set a flag to prevent multiple join attempts.
        _hasJoinedOnce = true;

        // Init and send join.
        Rejoin(timeoutMs);
        JoinPush.OnTimeout += joinPushTimeoutCallback;

        return tsc.Task;
    }

    /// <summary>
    /// Unsubscribes from the channel.
    /// </summary>
    public IRealtimeChannel Unsubscribe()
    {
        IsSubscribed = false;

        NotifyStateChanged(ChannelState.Leaving);

        var leavePush = new Push(Socket, this, ChannelEventLeave);
        leavePush.Send();

        NotifyStateChanged(ChannelState.Closed, false);

        return this;
    }

    /// <summary>
    /// Sends a `Push` request under this channel.
    /// 
    /// Maintains a buffer in the event push is called prior to the channel being joined.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="type"></param>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    public Push Push(string eventName, string? type = null, object? payload = null, int timeoutMs = DefaultTimeout)
    {
        if (!_hasJoinedOnce)
        {
            throw new RealtimeException(
                $"Tried to push '{eventName}' to '{Topic}' before joining. Use `Channel.Subscribe()` before pushing events")
            {
                Reason = FailureHint.Reason.ChannelNotOpen
            };
        }

        var push = new Push(Socket, this, eventName, type, payload, timeoutMs);
        Enqueue(push);

        return push;
    }

    /// <summary>
    /// Sends an arbitrary payload with a given payload type (<see cref="ChannelEventName"/>)
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="type"></param>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    public Task<bool> Send(ChannelEventName eventName, string? type, object payload, int timeoutMs = DefaultTimeout)
    {
        var tsc = new TaskCompletionSource<bool>();

        var ev = Core.Helpers.GetMappedToAttr(eventName).Mapping;
        var push = Push(ev, type, payload, timeoutMs);

        IRealtimePush<RealtimeChannel, SocketResponse>.MessageEventHandler? messageCallback = null;

        messageCallback = (_, message) =>
        {
            tsc.SetResult(message.Event != EventType.Unknown);
            push.RemoveMessageReceivedHandler(messageCallback!);
        };

        push.AddMessageReceivedHandler(messageCallback);
        return tsc.Task;
    }

    /// <summary>
    /// Rejoins the channel.
    /// </summary>
    /// <param name="timeoutMs"></param>
    public void Rejoin(int timeoutMs = DefaultTimeout)
    {
        if (IsLeaving) return;
        SendJoin(timeoutMs);
    }

    /// <summary>
    /// Enqueues a message.
    /// </summary>
    /// <param name="push"></param>
    internal void Enqueue(Push push)
    {
        LastPush = push;

        if (CanPush)
        {
            LastPush.Send();
        }
        else
        {
            LastPush.StartTimeout();
            _buffer.Add(LastPush);
        }
    }

    /// <summary>
    /// Generates the Join Push message by merging broadcast, presence, and postgres_changes options.
    /// </summary>
    /// <returns></returns>
    private Push GenerateJoinPush() => new(Socket, this, ChannelEventJoin,
        payload: new JoinPush(BroadcastOptions, PresenceOptions, PostgresChangesOptions));

    /// <summary>
    /// Generates an auth push.
    /// </summary>
    /// <returns></returns>
    private Push? GenerateAuthPush()
    {
        var accessToken = Options.RetrieveAccessToken();

        if (!string.IsNullOrEmpty(accessToken))
        {
            return new Push(Socket, this, ChannelAccessToken, payload: new Dictionary<string, string>
            {
                { "access_token", accessToken! }
            });
        }

        return null;
    }

    /// <summary>
    /// If the channel errors internally (phoenix error, not transport) attempt rejoining.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HandleRejoinTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (_isRejoining) return;
        _isRejoining = true;

        if (State != ChannelState.Closed && State != ChannelState.Errored)
            return;

        Debugger.Instance.Log(this, $"Rejoin Timer Elapsed: Attempting to rejoin [{Topic}]");

        // Reset join push instance
        JoinPush = GenerateJoinPush();

        Rejoin();
    }

    /// <summary>
    /// Sends the phoenix server a join message.
    /// </summary>
    /// <param name="timeoutMs"></param>
    private void SendJoin(int timeoutMs = DefaultTimeout)
    {
        NotifyStateChanged(ChannelState.Joining);

        // Remove handler if exists
        JoinPush?.RemoveMessageReceivedHandler(HandleJoinResponse);

        JoinPush = GenerateJoinPush();
        JoinPush.AddMessageReceivedHandler(HandleJoinResponse);
        JoinPush.Resend(timeoutMs);
    }

    /// <summary>
    /// Handles a received join response (received after sending on subscribe/reconnection)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    private void HandleJoinResponse(IRealtimePush<RealtimeChannel, SocketResponse> sender, SocketResponse message)
    {
        if (message._event != ChannelEventReply) return;

        var obj = JsonConvert.DeserializeObject<SocketResponse<PhoenixResponse>>(message.Json!,
            Options.SerializerSettings);
        if (obj?.Payload == null) return;

        obj.Payload.Response?.change?.ForEach(BindIdPostgresChanges);
        
        switch (obj.Payload.Status)
        {
            // A response was received from the channel
            case PhoenixStatusOk:
                // Disable Rejoin Timeout
                _rejoinTimer.Stop();
                _isRejoining = false;

                var authPush = GenerateAuthPush();
                authPush?.Send();

                // If postgres_changes options are specified, we need to wait for a system event
                // that registers a successful subscription (see HandleSocketMessage.System)
                if (PostgresChangesOptions.Count == 0)
                    NotifyStateChanged(ChannelState.Joined);
                break;
            case PhoenixStatusError:
                _rejoinTimer.Stop();
                _isRejoining = false;

                NotifyErrorOccurred(new RealtimeException(message.Json)
                    { Reason = FailureHint.Reason.ChannelJoinFailure });
                break;
        }
    }

    /// <summary>
    /// Called when a socket message is received, parses the correct event handler to pass to.
    /// </summary>
    /// <param name="message"></param>
    internal void HandleSocketMessage(SocketResponse message)
    {
        if (message.Ref == JoinPush?.Ref) return;

        // If we don't ignore this event we'll end up with double callbacks.
        if (message._event == "*") return;

        NotifyMessageReceived(message);

        switch (message.Event)
        {
            // If a channel is subscribed to postgres changes then we have a special case to account for:
            // A system event is emitted after the normal join ACK that says:
            // {"event":"system","payload":{"channel":"public:todos","extension":"postgres_changes","message":"Subscribed to PostgreSQL","status":"ok"}}
            // This switch case emits the join event after this has been received.
            case EventType.System:
                if (!IsJoining) return;

                var obj = JsonConvert.DeserializeObject<SocketResponse<PhoenixResponse>>(message.Json!,
                    Options.SerializerSettings);

                if (obj?.Payload == null) return;

                switch (obj.Payload.Status)
                {
                    case PhoenixStatusOk:
                        NotifyStateChanged(ChannelState.Joined);
                        break;
                    case PhoenixStatusError:
                        NotifyErrorOccurred(new RealtimeException(message.Json)
                            { Reason = FailureHint.Reason.ChannelJoinFailure });
                        break;
                }

                break;
            // Handles Insert, Update, Delete
            case EventType.PostgresChanges:
                var deserialized =
                    JsonConvert.DeserializeObject<PostgresChangesResponse>(message.Json!,
                        Options.SerializerSettings);

                if (deserialized?.Payload?.Data == null) return;

                deserialized.Json = message.Json;
                deserialized.SerializerSettings = Options.SerializerSettings;

                // Invoke '*' listener
                NotifyPostgresChanges(deserialized.Payload!.Data!.Type, deserialized);

                break;
            case EventType.Broadcast:
                BroadcastHandler?.Invoke(this, message);
                break;
            case EventType.PresenceState:
                PresenceSync?.Invoke(this, message);
                break;
            case EventType.PresenceDiff:
                PresenceDiff?.Invoke(this, message);
                break;
        }
    }

    /// <summary>
    /// Create a Binding and add to a list
    /// </summary>
    /// <param name="options"></param>
    private void BindPostgresChangesOptions(PostgresChangesOptions options)
    {
        var founded = _bindings.FirstOrDefault(b => options.Equals(b.Options));
        if (founded != null) return;
        
        _bindings.Add(
            new Binding
            {
                Options = options,
            }
        );
    }

    /// <summary>
    /// Try to bind a PostgresChangesHandler to a PostgresChangesOptions
    /// </summary>
    /// <param name="listenType"></param>
    /// <param name="handler"></param>
    private void BindPostgresChangesHandler(ListenType listenType, PostgresChangesHandler handler)
    {
        var founded = _bindings.FirstOrDefault(b =>
            b.Options?.Event == Core.Helpers.GetMappedToAttr(listenType).Mapping &&
            b.Handler == null
        );
        if (founded != null)
        {
            founded.Handler = handler;
            founded.ListenType = listenType;
            return;
        }

        BindPostgresChangesHandlerGeneric(listenType, handler);
        
    }

    private void BindPostgresChangesHandlerGeneric(ListenType listenType, PostgresChangesHandler handler)
    {
        var founded = _bindings.FirstOrDefault(b =>
            (b.Options?.Event == Core.Helpers.GetMappedToAttr(listenType).Mapping || b.Options?.Event == "*") &&
            b.Handler == null
        );
        if (founded == null) return;

        founded.Handler = handler;
        founded.ListenType = listenType;
    }

    /// <summary>
    /// Filter the binding list and try to add an id from socket to its binding
    /// </summary>
    /// <param name="joinResponse"></param>
    private void BindIdPostgresChanges(PhoenixPostgresChangeResponse joinResponse)
    {
        var founded = _bindings.FirstOrDefault(b => b.Options != null &&
                                                    b.Options.Event == joinResponse.eventName &&
                                                    b.Options.Table == joinResponse.table &&
                                                    b.Options.Schema == joinResponse.schema &&
                                                    b.Options.Filter == joinResponse.filter);
        if (founded == null) return;
        founded.Id = joinResponse?.id;
    }

    /// <summary>
    /// Try to invoke the handler properly based on event type and socket response 
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="response"></param>
    private void InvokeProperlyHandlerFromBind(ListenType eventType, PostgresChangesResponse response)
    {
        var all = _bindings.FirstOrDefault(b =>
        {
            if (b.Options == null && response.Payload == null && b.Handler == null) return false;

            return response.Payload != null && response.Payload.Ids.Contains(b.Id) && eventType != ListenType.All &&
                   b.ListenType == ListenType.All;
        });

        if (all != null)
        {
            all.Handler?.Invoke(this, response);
            return;
        }

        // Invoke all specific handler if possible
        _bindings.ForEach(binding =>
        {
            if (binding.ListenType != eventType) return;
            if (binding.Options == null || response.Payload == null || binding.Handler == null) return;
            
            if (response.Payload.Ids.Contains(binding.Id)) binding.Handler.Invoke(this, response);
        });
    }
    
    /// <summary>
    /// Remove handler from binding
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    private void RemovePostgresChangesFromBinding(ListenType eventType, PostgresChangesHandler handler)
    {
        var binding = _bindings.FirstOrDefault(b => b.Handler == handler && b.ListenType == eventType);
        if (binding == null) return;
        _bindings.Remove(binding);
    }
}
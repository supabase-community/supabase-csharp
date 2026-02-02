using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Supabase.Realtime.Channel;
using Supabase.Realtime.Exceptions;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;
using Supabase.Realtime.Socket;
using Websocket.Client.Exceptions;
using static Supabase.Realtime.Constants;

#pragma warning disable CS1570

namespace Supabase.Realtime;

/// <summary>
/// Singleton that represents a Client connection to a Realtime Server.
///
/// It maintains a singular Websocket with asynchronous listeners (RealtimeChannels).
/// </summary>
/// <example>
///     client = Client.Instance
/// </example>
[SuppressMessage("ReSharper", "InvalidXmlDocComment")]
public class Client : IRealtimeClient<RealtimeSocket, RealtimeChannel>
{
    /// <summary>
    /// Exposes all Realtime RealtimeChannel Subscriptions for R/O public consumption 
    /// </summary>
    public ReadOnlyDictionary<string, RealtimeChannel> Subscriptions => new(_subscriptions);

    /// <summary>
    /// The backing Socket class.
    ///
    /// Most methods of the Client act as proxies to the Socket class.
    /// </summary>
    public IRealtimeSocket? Socket { get; private set; }

    /// <summary>
    /// Client Options - most of which are regarding Socket connection Options
    /// </summary>
    public ClientOptions Options { get; }

    private Func<Dictionary<string, string>>? _getHeaders { get; set; }
    
    /// <inheritdoc />
    public Func<Dictionary<string, string>>? GetHeaders
    {
        get => _getHeaders;
        set
        {
            _getHeaders = value;
            
            if (Socket != null)
                Socket.GetHeaders = value;
        }
    }

    /// <summary>
    /// Custom Serializer resolvers and converters that will be used for encoding and decoding Postgrest JSON responses.
    ///
    /// By default, Postgrest seems to use a date format that C# and Newtonsoft do not like, so this initial
    /// configuration handles that.
    /// </summary>
    public JsonSerializerSettings SerializerSettings =>
        new()
        {
            ContractResolver = new CustomContractResolver(),
            Converters =
            {
                // 2020-08-28T12:01:54.763231
                new IsoDateTimeConverter
                {
                    DateTimeStyles = Options.DateTimeStyles,
                    DateTimeFormat = Options.DateTimeFormat
                }
            },
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

    /// <summary>
    /// JWT Access token for WALRUS security
    /// </summary>
    private string? AccessToken { get; set; }

    private readonly string _realtimeUrl;

    /// <summary>
    /// Handlers for notifications of state changes.
    /// </summary>
    private readonly List<IRealtimeClient<RealtimeSocket, RealtimeChannel>.SocketStateEventHandler>
        _socketEventHandlers = new();

    /// <summary>
    /// Contains all Realtime RealtimeChannel Subscriptions - state managed internally.
    ///
    /// Keys are of encoded value: `{database}{:schema?}{:table?}{:col.eq.:value?}`
    /// Values are of type `RealtimeChannel<T> where T : BaseModel, new()`;
    /// </summary>
    private readonly Dictionary<string, RealtimeChannel> _subscriptions;

    /// <summary>
    /// Initializes a Client instance, this method should be called prior to any other method.
    /// </summary>
    /// <param name="realtimeUrl">The connection url (ex: "ws://localhost:4000/socket" - no trailing slash required)</param>
    /// <param name="options"></param>
    /// <returns>Client</returns>
    public Client(string realtimeUrl, ClientOptions? options = null)
    {
        _realtimeUrl = realtimeUrl;
        _subscriptions = new Dictionary<string, RealtimeChannel>();

        options ??= new ClientOptions();
        options.Encode ??= DefaultMessageEncoder;
        options.Decode ??= DefaultMessageDecoder;
        Options = options;
    }


    /// <summary>
    /// Attempts to connect to the Socket.
    ///
    /// Returns when socket has successfully connected.
    /// </summary>
    /// <returns></returns>
    public async Task<IRealtimeClient<RealtimeSocket, RealtimeChannel>> ConnectAsync()
    {
        var tsc = new TaskCompletionSource<IRealtimeClient<RealtimeSocket, RealtimeChannel>>();

        if (Socket != null)
        {
            Debugger.Instance.Log(this, "Calling `ConnectAsync` on an instance that already has a `Socket`");
            tsc.SetResult(this);
        }

        Socket = new RealtimeSocket(_realtimeUrl, Options);
        Socket.GetHeaders = GetHeaders;

        IRealtimeSocket.StateEventHandler? socketStateHandler = null;
        socketStateHandler = (sender, state) =>
        {
            if (state != SocketState.Open) return;

            sender.RemoveStateChangedHandler(socketStateHandler!);

            Socket.AddMessageReceivedHandler(HandleSocketMessageReceived);
            Socket.AddHeartbeatHandler(HandleSocketHeartbeat);

            NotifySocketStateChange(SocketState.Open);
            tsc.TrySetResult(this);
        };

        Socket.AddStateChangedHandler(socketStateHandler);

        try
        {
            await Socket.Connect();
            await tsc.Task;
        }
        catch (WebsocketException ex)
        {
            Socket = null;
            throw new RealtimeException(ex.Message, ex) { Reason = FailureHint.Reason.SocketError };
        }

        return this;
    }

    /// <summary>
    /// Attempts to connect to the socket.
    ///
    /// Provides a callback for `Task` driven returns.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    [Obsolete("Please use ConnectAsync() instead.")]
    public IRealtimeClient<RealtimeSocket, RealtimeChannel> Connect(
        Action<IRealtimeClient<RealtimeSocket, RealtimeChannel>, RealtimeException?>? callback = null)
    {
        if (Socket != null)
        {
            Debugger.Instance.Log(this, "Calling `ConnectAsync` on an instance that already has a `Socket`");
            callback?.Invoke(this, null);
            return this;
        }

        Socket = new RealtimeSocket(_realtimeUrl, Options);
        IRealtimeSocket.StateEventHandler? socketStateHandler = null;
        IRealtimeSocket.ErrorEventHandler? errorEventHandler = null;

        socketStateHandler = (sender, state) =>
        {
            if (state != SocketState.Open) return;

            Socket.AddMessageReceivedHandler(HandleSocketMessageReceived);
            Socket.AddHeartbeatHandler(HandleSocketHeartbeat);

            sender.RemoveStateChangedHandler(socketStateHandler!);
            sender.RemoveErrorHandler(errorEventHandler!);

            NotifySocketStateChange(SocketState.Open);

            callback?.Invoke(this, null);
        };

        errorEventHandler = (sender, ex) =>
        {
            Socket = null;
            callback?.Invoke(this, ex);
        };

        Socket.AddStateChangedHandler(socketStateHandler);
        Socket.AddErrorHandler(errorEventHandler);
        Socket.Connect();

        return this;
    }

    /// <summary>
    /// Adds a listener to be notified when the socket state changes.
    /// </summary>
    public void AddStateChangedHandler(
        IRealtimeClient<RealtimeSocket, RealtimeChannel>.SocketStateEventHandler handler)
    {
        if (_socketEventHandlers.Contains(handler))
            return;

        _socketEventHandlers.Add(handler);
    }

    /// <summary>
    /// Removes a specified listener from socket state changes.
    /// </summary>
    public void RemoveStateChangedHandler(
        IRealtimeClient<RealtimeSocket, RealtimeChannel>.SocketStateEventHandler handler)
    {
        if (!_socketEventHandlers.Contains(handler))
            return;

        _socketEventHandlers.Remove(handler);
    }

    /// <summary>
    /// Clears all of the listeners from receiving socket state changes.
    /// </summary>
    public void ClearStateChangedHandlers() =>
        _socketEventHandlers.Clear();

    /// <summary>
    /// Notifies all listeners that the current user auth state has changed.
    ///
    /// This is mainly used internally to fire notifications - most client applications won't need this.
    /// </summary>
    /// <param name="stateChanged"></param>
    private void NotifySocketStateChange(SocketState stateChanged)
    {
        foreach (var handler in _socketEventHandlers.ToArray())
            handler.Invoke(this, stateChanged);
    }

    /// <summary>
    /// Adds a debug handler, likely used within a logging solution of some kind.
    /// </summary>
    /// <param name="handler"></param>
    public void AddDebugHandler(IRealtimeDebugger.DebugEventHandler handler) =>
        Debugger.Instance.AddDebugHandler(handler);


    /// <summary>
    /// Removes a debug handler
    /// </summary>
    /// <param name="handler"></param>
    public void RemoveDebugHandler(IRealtimeDebugger.DebugEventHandler handler) =>
        Debugger.Instance.RemoveDebugHandler(handler);

    /// <summary>
    /// Clears debug handlers;
    /// </summary>
    public void ClearDebugHandlers() => Debugger.Instance.ClearDebugHandlers();


    /// <summary>
    /// Sets the current Access Token every heartbeat (see: https://github.com/supabase/realtime-js/blob/59bd47956ebe4e23b3e1a6c07f5fe2cfe943e8ad/src/RealtimeClient.ts#L437)
    /// </summary>
    private void HandleSocketHeartbeat(IRealtimeSocket sender, SocketResponse message)
    {
        if (!string.IsNullOrEmpty(AccessToken))
            SetAuth(AccessToken!);
    }

    /// <summary>
    /// Disconnects from the socket server (if connected).
    /// </summary>
    /// <param name="code">Status Code</param>
    /// <param name="reason">Reason for disconnect</param>
    /// <returns></returns>
    public IRealtimeClient<RealtimeSocket, RealtimeChannel> Disconnect(
        WebSocketCloseStatus code = WebSocketCloseStatus.NormalClosure, string reason = "Programmatic Disconnect")
    {
        if (Socket != null)
        {
            Socket.RemoveMessageReceivedHandler(HandleSocketMessageReceived);
            Socket.RemoveStateChangedHandler(HandleSocketStateChanged);
            Socket.Disconnect(code, reason);
            Socket = null;
        }

        return this;
    }

    /// <summary>
    /// Sets the JWT access token used for channel subscription authorization and Realtime RLS.
    /// Ref: https://github.com/supabase/realtime-js/pull/117 | https://github.com/supabase/realtime-js/pull/117
    /// </summary>
    /// <param name="jwt"></param>
    public void SetAuth(string jwt)
    {
        AccessToken = jwt;

        foreach (var channel in _subscriptions.Values)
        {
            // See: https://github.com/supabase/realtime-js/pull/126
            channel.Options.Parameters!["user_token"] = AccessToken;

            if (channel.HasJoinedOnce && channel.IsJoined)
            {
                channel.Push(Constants.ChannelAccessToken, payload: new Dictionary<string, string>
                {
                    { "access_token", AccessToken }
                });
            }
        }
    }

    /// <summary>
    /// Adds a RealtimeChannel subscription - if a subscription exists with the same signature, the existing subscription will be returned.
    /// </summary>
    /// <param name="channelName">The name of the Channel to join (totally arbitrary)</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public RealtimeChannel Channel(string channelName)
    {
        var topic = $"realtime:{channelName}";

        if (_subscriptions.TryGetValue(topic, out var channel))
            return channel;

        if (Socket == null)
            throw new Exception("Socket must exist, was `Connect` called?");

        var subscription = new RealtimeChannel(Socket!, topic,
            new ChannelOptions(Options, () => AccessToken, SerializerSettings));
        _subscriptions.Add(topic, subscription);

        return subscription;
    }

    /// <summary>
    /// Adds a RealtimeChannel subscription - if a subscription exists with the same signature, the existing subscription will be returned.
    /// </summary>
    /// <param name="database">Database to connect to, with Supabase this will likely be `realtime`.</param>
    /// <param name="schema">Postgres schema, usually `public`</param>
    /// <param name="table">Postgres table name</param>
    /// <param name="column">Postgres column name</param>
    /// <param name="value">Value the specified column should have</param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public RealtimeChannel Channel(string database = "realtime", string schema = "public", string table = "*",
        string? column = null, string? value = null, Dictionary<string, string>? parameters = null)
    {
        var key = Utils.GenerateChannelTopic(database, schema, table, column, value);

        if (_subscriptions.TryGetValue(key, out var channel))
            return channel;

        if (Socket == null)
            throw new Exception("Socket must exist, was `Connect` called?");

        var changesOptions = new PostgresChangesOptions(schema, table,
            filter: column != null && value != null ? $"{column}=eq.{value}" : null, parameters: parameters);
        var options = new ChannelOptions(Options, () => AccessToken, SerializerSettings);

        var subscription = new RealtimeChannel(Socket!, key, options);
        subscription.Register(changesOptions);

        _subscriptions.Add(key, subscription);

        return subscription;
    }

    /// <summary>
    /// Removes a channel subscription.
    /// </summary>
    /// <param name="channel"></param>
    public void Remove(RealtimeChannel channel)
    {
        if (_subscriptions.ContainsKey(channel.Topic))
        {
            if (channel.IsJoined)
                channel.Unsubscribe();

            _subscriptions.Remove(channel.Topic);
        }
    }

    /// <summary>
    /// The default socket message encoder, used to serialize <see cref="JoinPush"/> messages to the socket
    /// server.
    ///
    /// It is unlikely that this will be overriden by the developer.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="callback"></param>
    private void DefaultMessageEncoder(object payload, Action<string> callback)
    {
        callback(JsonConvert.SerializeObject(payload, SerializerSettings));
    }

    /// <summary>
    /// The default socket message decoder, used to deserialize messages from the socket server.
    /// Ref: <see cref="SocketResponse{T}"/>
    ///
    /// It is unlikely that this will be overriden by the developer. 
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="callback"></param>
    private void DefaultMessageDecoder(string payload, Action<SocketResponse?> callback)
    {
        var response = new SocketResponse(SerializerSettings);
        JsonConvert.PopulateObject(payload, response, SerializerSettings);
        callback(response);
    }

    private void HandleSocketMessageReceived(IRealtimeSocket sender, SocketResponse message)
    {
        if (message.Topic != null && _subscriptions.TryGetValue(message.Topic, out var subscription))
            subscription.HandleSocketMessage(message);
    }

    private void HandleSocketStateChanged(IRealtimeSocket sender, SocketState state)
    {
        switch (state)
        {
            case SocketState.Open:
                // Ref: https://github.com/supabase/realtime-js/pull/116/files
                if (!string.IsNullOrEmpty(AccessToken))
                    SetAuth(AccessToken!);

                NotifySocketStateChange(SocketState.Open);
                break;
            case SocketState.Reconnect:
                // Ref: https://github.com/supabase/realtime-js/pull/116/files
                if (!string.IsNullOrEmpty(AccessToken))
                    SetAuth(AccessToken!);

                NotifySocketStateChange(SocketState.Reconnect);
                break;
            default:
                NotifySocketStateChange(state);
                break;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Models;
using WebSocketSharp;
using static Supabase.Realtime.StateChangedEventArgs;

namespace Supabase.Realtime
{
    public class Socket<T> where T : BaseModel, new()
    {
        public EventHandler<StateChangedEventArgs> StateChanged;

        private string endpoint;
        private SocketOptions<T> options;
        private WebSocket connection;

        private Task heartbeatTask;
        private CancellationTokenSource hearbeatTokenSource;

        private bool hasPendingHeartbeat = false;
        private int pendingHeartbeatRef = 0;

        private Task reconnectTask;
        private CancellationTokenSource reconnectTokenSource;

        private List<Channel> channels = new List<Channel>();
        private List<Task> buffer = new List<Task>();
        private int reference = 0;

        private string endpointUrl
        {
            get
            {
                var parameters = new Dictionary<string, object> {
                    { "apikey", options.Parameters.ApiKey }
                };

                return string.Format($"{endpoint}?{Utils.QueryString(parameters)}");
            }
        }

        public Socket(string endpoint, SocketOptions<T> options = null)
        {
            this.endpoint = $"{endpoint}/{Constants.TANSPORT_WEBSOCKET}";

            if (options == null)
            {
                options = new SocketOptions<T>();
            }

            this.options = options;
        }

        public void Connect()
        {
            if (connection != null) return;

            connection = new WebSocket(endpointUrl);
            connection.WaitTime = options.LongPollerTimeout;
            connection.OnOpen += OnConnectionOpened;
            connection.OnMessage += OnConnectionMessage;
            connection.OnError += OnConnectionError;
            connection.OnClose += OnConnectionClosed;
            connection.Connect();
        }

        public void Disconnect(CloseStatusCode code = CloseStatusCode.Normal, string reason = "")
        {
            if (connection != null)
            {
                connection.OnClose -= OnConnectionClosed;
                connection.Close(code, reason);
                connection = null;
            }
        }

        public void Push(SocketMessage<T> data)
        {
            var task = new Task(() => options.Encode(data, data => connection.Send(data)));

            options.Logger("push", $"{data.Topic} {data.Event} ({data.Ref})", data.Payload);

            if (connection.IsAlive)
            {
                task.Start();
            }
            else
            {
                buffer.Add(task);
            }
        }

        public void Channel(string topic, object parameters) { }
        public void Remove(Channel channel) { }


        private void SendHeartbeat()
        {
            if (!connection.IsAlive) return;
            if (hasPendingHeartbeat)
            {
                hasPendingHeartbeat = false;
                options.Logger("transport", "heartbeat timeout. Attempting to re-establish connection.", null);
                connection.Close(CloseStatusCode.Normal, "heartbeat timeout");
                return;
            }
            pendingHeartbeatRef = MakeRef();

            Push(new SocketMessage<T> { Topic = "pheonix", Event = "heartbeat", Ref = pendingHeartbeatRef.ToString() });
        }

        private void OnConnectionOpened(object sender, EventArgs args)
        {
            options.Logger("transport", $"connected to ${endpointUrl}", null);

            FlushBuffer();

            if (reconnectTokenSource != null)
                reconnectTokenSource.Cancel();

            if (hearbeatTokenSource != null)
                hearbeatTokenSource.Cancel();

            hearbeatTokenSource = new CancellationTokenSource();
            heartbeatTask = Task.Run(async () =>
            {
                while (!hearbeatTokenSource.IsCancellationRequested)
                {
                    SendHeartbeat();
                    await Task.Delay(options.HeartbeatInterval, hearbeatTokenSource.Token);
                }
            }, hearbeatTokenSource.Token);


            StateChanged?.Invoke(sender, new StateChangedEventArgs(ConnectionState.Open, args));
        }

        private void OnConnectionMessage(object sender, MessageEventArgs args)
        {
            options.Decode(args.Data, decoded =>
            {
                this.options.Logger("receive", $"{decoded.Payload.Status} {decoded.Topic} {decoded.Event} ({decoded.Ref})", decoded.Payload);
            });

            StateChanged?.Invoke(sender, new StateChangedEventArgs(ConnectionState.Message, args));
        }

        private void OnConnectionError(object sender, ErrorEventArgs args)
        {
            StateChanged?.Invoke(sender, new StateChangedEventArgs(ConnectionState.Error, args));
        }

        private void OnConnectionClosed(object sender, CloseEventArgs args)
        {
            options.Logger("transport", "close", args);

            if (reconnectTokenSource != null)
                reconnectTokenSource.Cancel();

            reconnectTokenSource = new CancellationTokenSource();
            reconnectTask = Task.Run(async () =>
            {
                var tries = 1;
                while (!reconnectTokenSource.IsCancellationRequested)
                {
                    this.Disconnect();
                    this.Connect();
                    await Task.Delay(options.ReconnectAfterInterval(tries++), reconnectTokenSource.Token);
                }
            }, reconnectTokenSource.Token);

            StateChanged?.Invoke(sender, new StateChangedEventArgs(ConnectionState.Close, args));
        }

        private int MakeRef() => reference + 1 == reference ? 0 : reference + 1;

        private void FlushBuffer()
        {
            foreach (var item in buffer)
            {
                item.Start();
            }
            buffer.Clear();
        }

    }

    public class SocketOptions<T> where T : BaseModel, new()
    {
        // The function to encode outgoing messages. Defaults to JSON:
        public Action<object, Action<string>> Encode { get; set; } = (payload, callback) => callback(JsonConvert.SerializeObject(payload));

        // The function to decode incoming messages.
        public Action<string, Action<SocketMessage<T>>> Decode { get; set; } = (payload, callback) => callback(JsonConvert.DeserializeObject<SocketMessage<T>>(payload));

        public Action<string, string, object> Logger { get; set; } = (kind, msg, data) => Debug.WriteLine($"{kind}: {msg}, {0}", JsonConvert.SerializeObject(data, Formatting.Indented));

        // The Websocket Transport, for example WebSocket.
        public string Transport { get; set; } = Constants.TANSPORT_WEBSOCKET;

        // The default timeout in milliseconds to trigger push timeouts.
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(Constants.DEFAULT_TIMEOUT);

        // The interval to send a heartbeat message
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

        // The interval to reconnect
        public Func<int, TimeSpan> ReconnectAfterInterval { get; set; } = (tries) =>
        {
            var intervals = new int[] { 1, 2, 5, 10 };
            return TimeSpan.FromSeconds(tries < intervals.Length ? tries - 1 : 10);
        };

        // The maximum timeout of a long poll AJAX request.
        public TimeSpan LongPollerTimeout = TimeSpan.FromSeconds(20);

        public Dictionary<string, object> Headers = new Dictionary<string, object>();

        // The optional params to pass when connecting
        public SocketOptionsParameters Parameters = new SocketOptionsParameters();
    }

    public class SocketOptionsParameters
    {
        [JsonProperty("apikey")]
        public string ApiKey { get; set; }
    }

    public class SocketMessage<T> where T : BaseModel, new()
    {
        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("payload")]
        public T Payload { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public enum ConnectionState
        {
            Open,
            Close,
            Error,
            Message
        }

        public ConnectionState State { get; set; }
        public EventArgs Args { get; set; }

        public StateChangedEventArgs(ConnectionState state, EventArgs args)
        {
            State = state;
            Args = args;
        }
    }
}

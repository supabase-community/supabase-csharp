using Newtonsoft.Json;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeRealtimeClient : IRealtimeClient<Socket, Channel>
    {
        public ClientOptions Options => throw new NotImplementedException();

        public JsonSerializerSettings SerializerSettings => throw new NotImplementedException();

        public IRealtimeSocket Socket => throw new NotImplementedException();

        public ReadOnlyDictionary<string, Channel> Subscriptions => throw new NotImplementedException();

        public event EventHandler<SocketStateChangedEventArgs> OnClose;
        public event EventHandler<SocketStateChangedEventArgs> OnError;
        public event EventHandler<SocketStateChangedEventArgs> OnMessage;
        public event EventHandler<SocketStateChangedEventArgs> OnOpen;

        public Channel Channel(string database = "realtime", string schema = null, string table = null, string column = null, string value = null, Dictionary<string, string> parameters = null)
        {
            throw new NotImplementedException();
        }

        public IRealtimeClient<Socket, Channel> Connect(Action<IRealtimeClient<Socket, Channel>> callback = null)
        {
            throw new NotImplementedException();
        }

        public Task<IRealtimeClient<Socket, Channel>> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public IRealtimeClient<Socket, Channel> Disconnect(WebSocketCloseStatus code = WebSocketCloseStatus.NormalClosure, string reason = "Programmatic Disconnect")
        {
            throw new NotImplementedException();
        }

        public void Remove(Channel channel)
        {
            throw new NotImplementedException();
        }

        void IRealtimeClient<Socket, Channel>.SetAuth(string jwt)
        {
            throw new NotImplementedException();
        }
    }
}

using Newtonsoft.Json;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Socket;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeRealtimeClient : IRealtimeClient<RealtimeSocket, RealtimeChannel>
    {
        public ClientOptions Options => throw new NotImplementedException();

        public JsonSerializerSettings SerializerSettings => throw new NotImplementedException();

        public IRealtimeSocket Socket => throw new NotImplementedException();

        public ReadOnlyDictionary<string, RealtimeChannel> Subscriptions => throw new NotImplementedException();

        public event EventHandler<SocketStateChangedEventArgs> OnClose;
        public event EventHandler<SocketStateChangedEventArgs> OnError;
        public event EventHandler<SocketStateChangedEventArgs> OnMessage;
        public event EventHandler<SocketStateChangedEventArgs> OnOpen;
		public event EventHandler<SocketStateChangedEventArgs> OnReconnect;

		public RealtimeChannel Channel(string database = "realtime", string schema = null, string table = null, string column = null, string value = null, Dictionary<string, string> parameters = null)
        {
            throw new NotImplementedException();
        }

		public RealtimeChannel Channel(string channelName)
		{
			throw new NotImplementedException();
		}

		public IRealtimeClient<RealtimeSocket, RealtimeChannel> Connect(Action<IRealtimeClient<RealtimeSocket, RealtimeChannel>> callback = null)
        {
            throw new NotImplementedException();
        }

        public Task<IRealtimeClient<RealtimeSocket, RealtimeChannel>> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public IRealtimeClient<RealtimeSocket, RealtimeChannel> Disconnect(WebSocketCloseStatus code = WebSocketCloseStatus.NormalClosure, string reason = "Programmatic Disconnect")
        {
            throw new NotImplementedException();
        }

        public void Remove(RealtimeChannel channel)
        {
            throw new NotImplementedException();
        }

        void IRealtimeClient<RealtimeSocket, RealtimeChannel>.SetAuth(string jwt)
        {
            throw new NotImplementedException();
        }
    }
}

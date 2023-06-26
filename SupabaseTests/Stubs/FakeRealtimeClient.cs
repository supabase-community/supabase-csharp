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
using Supabase.Realtime.Exceptions;

namespace SupabaseTests.Stubs
{
    internal class FakeRealtimeClient : IRealtimeClient<RealtimeSocket, RealtimeChannel>
    {
        public void AddStateChangedHandler(IRealtimeClient<RealtimeSocket, RealtimeChannel>.SocketStateEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void RemoveStateChangedHandler(IRealtimeClient<RealtimeSocket, RealtimeChannel>.SocketStateEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void ClearStateChangedHandlers()
        {
            throw new NotImplementedException();
        }

        public void AddDebugHandler(IRealtimeDebugger.DebugEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void RemoveDebugHandler(IRealtimeDebugger.DebugEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public void ClearDebugHandlers()
        {
            throw new NotImplementedException();
        }

        public RealtimeChannel Channel(string channelName)
        {
            throw new NotImplementedException();
        }

        public RealtimeChannel Channel(string database = "realtime", string schema = "public", string table = "*",
            string column = null, string value = null, Dictionary<string, string> parameters = null)
        {
            throw new NotImplementedException();
        }

        public IRealtimeClient<RealtimeSocket, RealtimeChannel> Connect(Action<IRealtimeClient<RealtimeSocket, RealtimeChannel>, RealtimeException> callback = null)
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

        public void SetAuth(string jwt)
        {
            throw new NotImplementedException();
        }

        public ClientOptions Options { get; }
        public JsonSerializerSettings SerializerSettings { get; }
        public IRealtimeSocket Socket { get; }
        public ReadOnlyDictionary<string, RealtimeChannel> Subscriptions { get; }
    }
}

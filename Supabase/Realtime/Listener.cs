using System;
using System.Diagnostics;
using Postgrest.Models;
using static Supabase.Realtime.Constants;
using static Supabase.Realtime.StateChangedEventArgs;

namespace Supabase.Realtime
{
    public class Listener<T> where T : BaseModel, new()
    {
        private string tableName;
        private string realtimeUrl;
        private string schema;
        private ClientAuthorization authorization;

        private string uuid;

        private Socket<T> socket;
        private string channel;
        private object listeners;
        private object queryFilters;

        public EventHandler<ItemInsertedEventArgs> OnInsert;
        public EventHandler<ItemUpdatedEventArgs> OnUpdated;
        public EventHandler<ItemDeletedEventArgs> OnDelete;

        public Listener(string tableName, string realtimeUrl, string schema, ClientAuthorization authorization, string uuid, EventType eventType, Action<T> callback, object queryFilters)
        {
            this.tableName = tableName;
            this.realtimeUrl = realtimeUrl;
            this.schema = schema;
            this.authorization = authorization;

            this.uuid = uuid;

            this.queryFilters = queryFilters;

            On(eventType, callback);
        }

        public void CreateListener()
        {
            var socketUrl = this.realtimeUrl;

            var filterString = "";

            var channel = tableName == "*" ? "realtime:*" : $"realtime:{schema}:{tableName}{filterString}";

            // TODO: Other auth options?
            socket = new Realtime.Socket<T>(socketUrl, new SocketOptions<T> { Parameters = new SocketOptionsParameters { ApiKey = authorization.ApiKey } });
            socket.StateChanged += (object sender, StateChangedEventArgs args) =>
            {
                switch (args.State)
                {
                    case ConnectionState.Open:
                        Debug.WriteLine($"{realtimeUrl}: REALTIME CONNECTED");
                        break;
                    case ConnectionState.Close:
                        Debug.WriteLine($"{realtimeUrl}: REALTIME DISCONNECTED");
                        break;
                }
            };
            socket.Connect();
        }

        public void On(EventType eventType, Action<T> callback)
        {
            if (socket == null)
                CreateListener();

        }

        public Listener<T> Subscribe() { return this; }
        public Listener<T> Unsubscribe() { return this; }
    }

    public class ItemInsertedEventArgs : EventArgs { }
    public class ItemUpdatedEventArgs : EventArgs { }
    public class ItemDeletedEventArgs : EventArgs { }
}

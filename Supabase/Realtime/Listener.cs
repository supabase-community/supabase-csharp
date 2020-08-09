using System;
using System.Diagnostics;
using Supabase.Models;
using static Supabase.Realtime.StateChangedEventArgs;

namespace Supabase.Realtime
{
    public class Listener<T> where T : BaseModel, new()
    {
        private string tableName;
        private string realtimeUrl;
        private string schema;
        private string apiKey;
        private string uuid;

        private Socket<T> socket;
        private string channel;
        private object listeners;
        private object queryFilters;

        public EventHandler<ItemInsertedEventArgs> OnInsert;
        public EventHandler<ItemUpdatedEventArgs> OnUpdated;
        public EventHandler<ItemDeletedEventArgs> OnDelete;

        public Listener(string tableName, string realtimeUrl, string schema, string apiKey, string uuid, string eventType, Action<T> callback, object queryFilters)
        {
            this.tableName = tableName;
            this.realtimeUrl = realtimeUrl;
            this.schema = schema;
            this.apiKey = apiKey;
            this.uuid = uuid;

            this.queryFilters = queryFilters;

        }

        public void CreateListener()
        {
            var socketUrl = this.realtimeUrl;

            var filterString = "";

            var channel = tableName == "*" ? "realtime:*" : $"realtime:{schema}:{tableName}{filterString}";

            socket = new Realtime.Socket<T>(socketUrl, new SocketOptions<T> { Parameters = new SocketOptionsParameters { ApiKey = apiKey } });
            socket.StateChanged += (object sender, StateChangedEventArgs args) =>
            {
                switch (args.State)
                {
                    case ConnectionState.Open:
                        Debug.WriteLine($"{this.realtimeUrl}: REALTIME CONNECTED");
                        break;
                    case ConnectionState.Close:
                        Debug.WriteLine($"{this.realtimeUrl}: REALTIME DISCONNECTED");
                        break;
                }
            };
        }

        public Listener<T> Subscribe() { return this; }
        public Listener<T> Unsubscribe() { return this; }
    }

    public class ItemInsertedEventArgs : EventArgs { }
    public class ItemUpdatedEventArgs : EventArgs { }
    public class ItemDeletedEventArgs : EventArgs { }
}

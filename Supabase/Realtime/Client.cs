using System;
using System.Collections.Generic;
using Supabase.Models;
using Supabase.Postgrest;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime
{
    public class Client<T> where T : BaseModel, new()
    {
        private Dictionary<string, object> subscriptions;

        private string tableName;
        private object queryFiters;

        private string realtimeUrl;
        private ClientAuthorization authorization;
        private ClientOptions options;


        public Client(string realtimeUrl, string supabaseKey, string schema)
        {
            
        }

        public Client(string realtimeUrl, ClientAuthorization authorization, ClientOptions options)
        {
            this.realtimeUrl = realtimeUrl;
            this.authorization = authorization;
            this.options = options;
            this.subscriptions = new Dictionary<string, dynamic>();

            var attr = Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            if (attr is TableAttribute tableAttr)
            {
                tableName = tableAttr.Name;
            }
            else
            {
                tableName = typeof(T).Name;
            }
        }

        public Client<T> From(string tableName)
        {
            this.tableName = tableName;
            return this;
        }

        public void Clear()
        {
            tableName = null;
        }

        // REALTIME

        public Listener<T> On(EventType eventType, Action<T> callback) 
        {
            var id = Guid.NewGuid().ToString();
            subscriptions.Add(id, new Listener<T>(tableName, realtimeUrl, options.Schema, authorization, id, eventType, callback, queryFiters));

            Clear();

            return subscriptions[id] as Realtime.Listener<T>;
        }
    }
}

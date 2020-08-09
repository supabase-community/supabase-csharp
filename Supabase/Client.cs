using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Supabase.Models;
using Supabase.Postgrest.Responses;
using Supabase.Realtime;

namespace Supabase
{
    public class Client
    {
        private string supabaseUrl;
        private string supabaseKey;
        private string restUrl;
        private string realtimeUrl;
        private string authUrl;
        private string schema;

        private Dictionary<string, object> subscriptions;

        private string tableName;
        private object queryFiters;

        private static Client instance;
        public static Client Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.WriteLine("Supabase must be initialized before it is called.");
                    return null;
                }
                return instance;
            }
        }

        public static Client Initialize(string supabaseUrl, string supabaseKey, SupabaseOptions options = null)
        {
            if (options == null)
                options = new SupabaseOptions();

            instance = new Client();
            instance.Authenticate(supabaseUrl, supabaseKey);
            instance.schema = options.schema;
            instance.subscriptions = new Dictionary<string, dynamic>();

            return instance;
        }


        public void Authenticate(string supabaseUrl, string supabaseKey)
        {
            this.supabaseUrl = supabaseUrl;
            this.supabaseKey = supabaseKey;
            this.restUrl = $"{this.supabaseUrl}/rest/v1";
            this.realtimeUrl = $"{this.supabaseUrl}/realtime/v1".Replace("http", "ws");
            this.authUrl = $"{this.supabaseUrl}/auth/v1";
        }

        public Client From(string tableName)
        {
            this.tableName = tableName;
            return this;
        }

        // REALTIME

        public Listener<T> On<T>(string eventType, Action<T> callback) where T : BaseModel, new()
        {
            var id = Guid.NewGuid().ToString();
            subscriptions.Add(id, new Listener<T>(tableName, realtimeUrl, schema, supabaseKey, id, eventType, callback, queryFiters));

            Clear();

            return subscriptions[id] as Realtime.Listener<T>;
        }

        // REST API
        public Postgrest.Client Select(string columnQuery = "*")
        {
            var client = GetClient();

            return client.Select(columnQuery);
        }

        public Task<ModeledResponse<T>> Insert<T>(T model) where T : BaseModel, new()
        {
            var client = GetClient();
            return client.Insert(model);
        }

        public async Task<T> Update<T>(T model) where T : BaseModel, new()
        {
            var client = GetClient();

            await client.Insert(model);

            return null;
        }

        private Postgrest.Client GetClient()
        {
            var client = new Postgrest.Client(restUrl, supabaseKey, new Postgrest.ClientOptions
            {
                Headers = new Dictionary<string, object> { { "Authorization", $"Bearer {supabaseKey}" } },
                Schema = schema
            });

            client.From(tableName);

            return client;
        }

        private void Clear()
        {
            this.tableName = null;
            this.queryFiters = null;
        }
    }

    public class SupabaseOptions
    {
        public bool autoRefreshToken = true;
        public string schema = "public";
    }
}

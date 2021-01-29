using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Models;
using Postgrest.Responses;

namespace Supabase
{
    /// <summary>
    /// A singleton class representing a Supabase Client.
    /// </summary>
    public class Client
    {
        private string restUrl;
        private string realtimeUrl;
        private string authUrl;
        private string schema;

        private Postgrest.Client pgClient;

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

        private Client() { }

        /// <summary>
        /// Initializes a Supabase Client.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Client Initialize(string supabaseUrl, string supabaseKey, SupabaseOptions options = null)
        {
            instance = new Client();

            if (options == null)
                options = new SupabaseOptions();

            instance.restUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            instance.realtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
            instance.authUrl = string.Format(options.AuthUrlFormat, supabaseUrl);
            instance.schema = options.Schema;

            instance.pgClient = Postgrest.Client.Instance.Initialize(instance.restUrl, new ClientAuthorization(supabaseKey));

            return instance;
        }

        /// <summary>
        /// Gets the Postgrest client to prepare for a query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Table<T> From<T>() where T : BaseModel, new() => pgClient.Table<T>();

        /// <summary>
        /// Runs a remote procedure.
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters) => pgClient.Rpc(procedureName, parameters);
    }

    /// <summary>
    /// Options available for Supabase Client Configuration
    /// </summary>
    public class SupabaseOptions
    {
        public string Schema = "public";
        public bool AutoRefreshToken = true;

        public Action<object> PersistSession = (object arg) => { };
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public string RestUrlFormat { get; set; } = "{0}/rest/v1";
        public string RealtimeUrlFormat { get; set; } = "{0}/realtime/v1";
        public string AuthUrlFormat { get; set; } = "{0}/auth/v1";
    }
}

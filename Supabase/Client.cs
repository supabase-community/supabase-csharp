using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Gotrue;

namespace Supabase
{
    /// <summary>
    /// A singleton class representing a Supabase Client.
    /// </summary>
    public class Client
    {
        public enum ChannelEventType
        {
            Insert,
            Update,
            Delete,
            All
        }

        public Gotrue.Client Auth { get; private set; }
        public Realtime.Client Realtime { get; private set; }

        private Postgrest.Client Postgrest() => global::Postgrest.Client.Initialize(instance.RestUrl, new Postgrest.ClientOptions
        {
            Headers = instance.GetAuthHeaders(),
            Schema = Schema
        });

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

        public string SupabaseUrl { get; private set; }
        public string SupabaseKey { get; private set; }
        public string RestUrl { get; private set; }
        public string RealtimeUrl { get; private set; }
        public string AuthUrl { get; private set; }
        public string Schema { get; private set; }

        private SupabaseOptions options;

        private Client() { }


        /// <summary>
        /// Initializes a Supabase Client.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static void Initialize(string supabaseUrl, string supabaseKey, SupabaseOptions options = null, Action<Client> callback = null)
        {
            Task.Run(async () =>
            {
                var result = await InitializeAsync(supabaseUrl, supabaseKey, options);
                callback?.Invoke(result);
            });
        }

        /// <summary>
        /// Initializes a Supabase Client Asynchronously.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<Client> InitializeAsync(string supabaseUrl, string supabaseKey, SupabaseOptions options = null)
        {
            instance = new Client();

            instance.SupabaseUrl = supabaseUrl;
            instance.SupabaseKey = supabaseKey;

            if (options == null)
                options = new SupabaseOptions();

            instance.options = options;
            instance.RestUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            instance.RealtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
            instance.AuthUrl = string.Format(options.AuthUrlFormat, supabaseUrl);
            instance.Schema = options.Schema;

            instance.Auth = await Gotrue.Client.InitializeAsync(new Gotrue.ClientOptions
            {
                Url = instance.AuthUrl,
                Headers = instance.GetAuthHeaders(),
                AutoRefreshToken = options.AutoRefreshToken,
                PersistSession = options.PersistSession,
                SessionDestroyer = options.SessionDestroyer,
                SessionPersistor = options.SessionPersistor,
                SessionRetriever = options.SessionRetriever
            });

            if (options.ShouldInitializeRealtime)
            {
                instance.Realtime = Supabase.Realtime.Client.Initialize(instance.RealtimeUrl, new Realtime.ClientOptions
                {
                    Parameters = { ApiKey = instance.SupabaseKey }
                });

                if (options.AutoConnectRealtime)
                {
                    await instance.Realtime.Connect();
                }
            }

            return instance;
        }

        /// <summary>
        /// Gets the Postgrest client to prepare for a query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public SupabaseTable<T> From<T>() where T : BaseModel, new() => new SupabaseTable<T>();

        /// <summary>
        /// Runs a remote procedure.
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters) => Postgrest().Rpc(procedureName, parameters);


        internal Dictionary<string, string> GetAuthHeaders()
        {
            var headers = new Dictionary<string, string>();
            headers["apiKey"] = SupabaseKey;
            headers["X-Client-Info"] = Util.GetAssemblyVersion();

            // In Regard To: https://github.com/supabase/supabase-csharp/issues/5
            if (options.Headers.ContainsKey("Authorization"))
            {
                headers["Authorization"] = options.Headers["Authorization"];
            }
            else
            {
                var bearer = Auth?.CurrentSession?.AccessToken != null ? Auth.CurrentSession.AccessToken : SupabaseKey;
                headers["Authorization"] = $"Bearer {bearer}";
            }

            return headers;
        }
    }

    /// <summary>
    /// Options available for Supabase Client Configuration
    /// </summary>
    public class SupabaseOptions
    {
        public string Schema = "public";

        /// <summary>
        /// Should the Client automatically handle refreshing the User's Token?
        /// </summary>
        public bool AutoRefreshToken { get; set; } = true;

        /// <summary>
        /// Should the Client Initialize Realtime?
        /// </summary>
        public bool ShouldInitializeRealtime { get; set; } = false;

        /// <summary>
        /// Should the Client automatically connect to Realtime?
        /// </summary>
        public bool AutoConnectRealtime { get; set; } = false;

        /// <summary>
        /// Should the Client call <see cref="SessionPersistor"/>, <see cref="SessionRetriever"/>, and <see cref="SessionDestroyer"/>?
        /// </summary>
        public bool PersistSession { get; set; } = true;

        /// <summary>
        /// Function called to persist the session (probably on a filesystem or cookie)
        /// </summary>
        public Func<Session, Task<bool>> SessionPersistor = (Session session) => Task.FromResult<bool>(true);

        /// <summary>
        /// Function to retrieve a session (probably from the filesystem or cookie)
        /// </summary>
        public Func<Task<Session>> SessionRetriever = () => Task.FromResult<Session>(null);

        /// <summary>
        /// Function to destroy a session.
        /// </summary>
        public Func<Task<bool>> SessionDestroyer = () => Task.FromResult<bool>(true);

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public string RestUrlFormat { get; set; } = "{0}/rest/v1";
        public string RealtimeUrlFormat { get; set; } = "{0}/realtime/v1";
        public string AuthUrlFormat { get; set; } = "{0}/auth/v1";
    }
}

using System.Diagnostics;
using Supabase.Extensions;

namespace Supabase
{
    public class Client
    {
        private Auth.Client authClient;
        private Postgrest.Client databaseClient;
        private ClientAuthorization authorization;

        private SupabaseOptions options;

        private Realtime.ClientOptions realtimeOptions;
        private Postgrest.ClientOptions databaseOptions;

        private string restUrl;
        private string realtimeUrl;
        private string authUrl;

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

        public static Client Initialize(string supabaseUrl, ClientAuthorization authorization, SupabaseOptions options = null)
        {
            if (authorization == null)
                authorization = new ClientAuthorization();

            if (options == null)
                options = new SupabaseOptions();

            instance = new Client();
            instance.options = options;
            instance.Authenticate(supabaseUrl, authorization);

            return instance;
        }


        /// <summary>
        /// Returns either a new instance (if it has not been instantiated) or the existing instance of the Auth Client
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Auth.Client Auth(Auth.ClientOptions options = null)
        {
            if (authClient != null)
            {
                if (options != null)
                {
                    authClient.Options = options;
                }
                return authClient;
            }
            else
            {
                if (options == null)
                    options = new Auth.ClientOptions();

                authClient = new Auth.Client(authUrl, authorization, options);

                return authClient;
            }
        }

        /// <summary>
        /// Returns either a new instance (if it has not been instantiated) or the existing instance of the Database Client
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Postgrest.Builder<T> Database<T>(Postgrest.ClientOptions options = null) where T : Postgrest.Models.BaseModel, new()
        {
            if (databaseClient != null)
            {
                if (options != null)
                {
                    databaseClient.Initialize(restUrl, authorization.ToPostgrestAuth(), options);
                }
                return databaseClient.Builder<T>();
            }
            else
            {
                if (options == null)
                    options = new Postgrest.ClientOptions { Schema = this.options.Schema };

                databaseClient = Postgrest.Client.Instance.Initialize(restUrl, authorization.ToPostgrestAuth(), options);

                return databaseClient.Builder<T>();
            }
        }

        /// <summary>
        /// Returns a new instance of the Realtime Client
        /// </summary>
        /// <typeparam name="T">Model to be used as coercion type for the returned instance</typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public Realtime.Client<T> Realtime<T>(Realtime.ClientOptions options = null) where T : Postgrest.Models.BaseModel, new()
        {
            if (options == null && realtimeOptions == null)
                options = new Realtime.ClientOptions { Schema = this.options.Schema };

            realtimeOptions = options;

            return new Realtime.Client<T>(realtimeUrl, authorization, realtimeOptions);
        }

        /// <summary>
        /// Sets up urls and authorization options for this instance
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="authorization"></param>
        public void Authenticate(string supabaseUrl, ClientAuthorization authorization)
        {
            this.authorization = authorization;
            this.restUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            this.realtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
            this.authUrl = string.Format(options.AuthUrlFormat, supabaseUrl);
        }
    }

    public class SupabaseOptions
    {
        public string Schema = "public";

        public string RestUrlFormat { get; set; } = "{0}/rest/v1";
        public string RealtimeUrlFormat { get; set; } = "{0}/realtime/v1";
        public string AuthUrlFormat { get; set; } = "{0}/auth/v1";
    }
}

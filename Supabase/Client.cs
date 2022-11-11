using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using Storage.Interfaces;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Storage;
using static Supabase.Gotrue.Constants;

namespace Supabase
{
    /// <summary>
    /// A singleton class representing a Supabase Client.
    /// </summary>
    public class Client : ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject>
    {
        public enum ChannelEventType
        {
            Insert,
            Update,
            Delete,
            All
        }

        public IGotrueClient<User, Session> Auth { get => AuthClient; }
        public SupabaseFunctions Functions { get => new SupabaseFunctions(FunctionsClient, functionsUrl, GetAuthHeaders()); }

        public IPostgrestClient Postgrest { get => PostgrestClient; }

        public IRealtimeClient<Socket, Channel> Realtime { get => RealtimeClient; }

        public IStorageClient<Bucket, FileObject> Storage { get => StorageClient; }

        /// <summary>
        /// Supabase Auth allows you to create and manage user sessions for access to data that is secured by access policies.
        /// </summary>
        public IGotrueClient<User, Session> AuthClient
        {
            get
            {
                return _authClient;
            }
            set
            {
                // Remove existing internal state listener (if applicable)
                if (_authClient != null)
                    _authClient.StateChanged -= Auth_StateChanged;

                _authClient = value;
                _authClient.StateChanged += Auth_StateChanged;
            }
        }
        private IGotrueClient<User, Session> _authClient;

        /// <summary>
        /// Supabase Realtime allows for realtime feedback on database changes.
        /// </summary>
        public IRealtimeClient<Socket, Channel> RealtimeClient
        {
            get
            {
                return _realtimeClient;
            }
            set
            {
                // Disconnect from previous socket (if applicable)
                if (_realtimeClient != null)
                    _realtimeClient.Disconnect();

                _realtimeClient = value;
            }
        }
        private IRealtimeClient<Socket, Channel> _realtimeClient;

        /// <summary>
        /// Supabase Edge functions allow you to deploy and invoke edge functions.
        /// </summary>
        public IFunctionsClient FunctionsClient
        {
            get => _functionsClient;
            set => _functionsClient = value;
        }
        private IFunctionsClient _functionsClient;

        /// <summary>
        /// Supabase Postgrest allows for strongly typed REST interactions with the your database.
        /// </summary>
        public IPostgrestClient PostgrestClient
        {
            get => _postgrestClient;
            set => _postgrestClient = value;
        }
        private IPostgrestClient _postgrestClient;

        /// <summary>
        /// Supabase Storage allows you to manage user-generated content, such as photos or videos.
        /// </summary>
        public IStorageClient<Bucket, FileObject> StorageClient
        {
            get => _storageClient;
            set => _storageClient = value;
        }
        private IStorageClient<Bucket, FileObject> _storageClient;

        private string? supabaseKey;
        private string authUrl;
        private string restUrl;
        private string realtimeUrl;
        private string storageUrl;
        private string functionsUrl;
        private string schema;

        private SupabaseOptions options;

        public Client(string supabaseUrl, string? supabaseKey, SupabaseOptions? options = null)
        {

            this.supabaseKey = supabaseKey;

            options ??= new SupabaseOptions();
            this.options = options;

            authUrl = string.Format(options.AuthUrlFormat, supabaseUrl);
            restUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            realtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
            storageUrl = string.Format(options.StorageUrlFormat, supabaseUrl);
            schema = options.Schema;

            // See: https://github.com/supabase/supabase-js/blob/09065a65f171bc28a9fd7b831af2c24e5f1a380b/src/SupabaseClient.ts#L77-L83
            var isPlatform = new Regex(@"(supabase\.co)|(supabase\.in)").Match(supabaseUrl);

            if (isPlatform.Success)
            {
                var parts = supabaseUrl.Split('.');
                functionsUrl = $"{parts[0]}.functions.{parts[1]}.{parts[2]}";
            }
            else
            {
                functionsUrl = string.Format(options.FunctionsUrlFormat, supabaseUrl);
            }

            // Init Auth
            var gotrueOptions = new Gotrue.ClientOptions
            {
                Url = authUrl,
                Headers = GetAuthHeaders(),
                AutoRefreshToken = options.AutoRefreshToken,
                PersistSession = options.PersistSession,
                SessionDestroyer = options.SessionHandler.SessionDestroyer,
                SessionPersistor = options.SessionHandler.SessionPersistor,
                SessionRetriever = options.SessionHandler.SessionRetriever
            };

            _authClient = new Gotrue.Client(gotrueOptions);
            _authClient.StateChanged += Auth_StateChanged;

            // Init Realtime

            var realtimeOptions = new Realtime.ClientOptions
            {
                Parameters = { ApiKey = this.supabaseKey }
            };

            _realtimeClient = new Realtime.Client(realtimeUrl, realtimeOptions);

            _postgrestClient = new Postgrest.Client(restUrl, new Postgrest.ClientOptions
            {
                Headers = GetAuthHeaders(),
                Schema = schema
            });

            _functionsClient = new Functions.Client();

            _storageClient = new Storage.Client(storageUrl, GetAuthHeaders());
        }


        /// <summary>
        /// Initializes a Supabase Client.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject>> InitializeAsync()
        {
            await AuthClient.RetrieveSessionAsync();

            if (options.AutoConnectRealtime)
            {
                await RealtimeClient.ConnectAsync();
            }
            return this;
        }

        private void Auth_StateChanged(object sender, ClientStateChanged e)
        {
            switch (e.State)
            {
                // Pass new Auth down to Realtime
                // Ref: https://github.com/supabase-community/supabase-csharp/issues/12
                case AuthState.SignedIn:
                case AuthState.TokenRefreshed:
                    if (AuthClient.CurrentSession?.AccessToken != null)
                    {
                        RealtimeClient.SetAuth(AuthClient.CurrentSession.AccessToken);
                    }
                    _postgrestClient.Options.Headers = GetAuthHeaders();
                    _storageClient.Headers = GetAuthHeaders();
                    break;

                // Remove Realtime Subscriptions on Auth Signout.
                case AuthState.SignedOut:
                    foreach (var subscription in RealtimeClient.Subscriptions.Values)
                        subscription.Unsubscribe();
                    RealtimeClient.Disconnect();
                    break;
            }
        }

        /// <summary>
        /// Gets the Postgrest client to prepare for a query.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public ISupabaseTable<TModel, Channel> From<TModel>() where TModel : BaseModel, new() => new SupabaseTable<TModel>(Postgrest, Realtime);

        /// <summary>
        /// Runs a remote procedure.
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters) => Postgrest.Rpc(procedureName, parameters);

        internal Dictionary<string, string> GetAuthHeaders()
        {
            var headers = new Dictionary<string, string>();

            headers["X-Client-Info"] = Util.GetAssemblyVersion();

            if (supabaseKey != null)
            {
                headers["apiKey"] = supabaseKey;
            }

            // In Regard To: https://github.com/supabase/supabase-csharp/issues/5
            if (options.Headers.ContainsKey("Authorization"))
            {
                headers["Authorization"] = options.Headers["Authorization"];
            }
            else
            {
                var bearer = AuthClient.CurrentSession?.AccessToken != null ? AuthClient.CurrentSession.AccessToken : supabaseKey;
                headers["Authorization"] = $"Bearer {bearer}";
            }

            return headers;
        }
    }
}

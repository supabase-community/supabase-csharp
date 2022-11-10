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
        public SupabaseFunctions Functions { get => new SupabaseFunctions(FunctionsClient, FunctionsUrl, GetAuthHeaders()); }

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

        public string SupabaseKey { get; private set; }
        public string SupabaseUrl { get; private set; }
        public string AuthUrl { get; private set; }
        public string RestUrl { get; private set; }
        public string RealtimeUrl { get; private set; }
        public string StorageUrl { get; private set; }
        public string FunctionsUrl { get; private set; }
        public string Schema { get; private set; }

        private SupabaseOptions options;

        public Client(string supabaseUrl, string supabaseKey, SupabaseOptions? options = null)
        {

            SupabaseUrl = supabaseUrl;
            SupabaseKey = supabaseKey;

            options ??= new SupabaseOptions();
            this.options = options;

            AuthUrl = string.Format(options.AuthUrlFormat, supabaseUrl);
            RestUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            RealtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
            StorageUrl = string.Format(options.StorageUrlFormat, supabaseUrl);
            Schema = options.Schema;

            // See: https://github.com/supabase/supabase-js/blob/09065a65f171bc28a9fd7b831af2c24e5f1a380b/src/SupabaseClient.ts#L77-L83
            var isPlatform = new Regex(@"(supabase\.co)|(supabase\.in)").Match(supabaseUrl);

            if (isPlatform.Success)
            {
                var parts = supabaseUrl.Split('.');
                FunctionsUrl = $"{parts[0]}.functions.{parts[1]}.{parts[2]}";
            }
            else
            {
                FunctionsUrl = string.Format(options.FunctionsUrlFormat, supabaseUrl);
            }

            // Init Auth
            var gotrueOptions = new Gotrue.ClientOptions
            {
                Url = AuthUrl,
                Headers = GetAuthHeaders(),
                AutoRefreshToken = options.AutoRefreshToken,
                PersistSession = options.PersistSession,
                SessionDestroyer = options.SessionDestroyer,
                SessionPersistor = options.SessionPersistor,
                SessionRetriever = options.SessionRetriever
            };

            _authClient = new Gotrue.Client(gotrueOptions);
            _authClient.StateChanged += Auth_StateChanged;

            // Init Realtime

            var realtimeOptions = new Realtime.ClientOptions
            {
                Parameters = { ApiKey = SupabaseKey }
            };

            _realtimeClient = new Realtime.Client(RealtimeUrl, realtimeOptions);

            _postgrestClient = new Postgrest.Client(RestUrl, new Postgrest.ClientOptions
            {
                Headers = GetAuthHeaders(),
                Schema = Schema
            });

            _functionsClient = new Functions.Client();

            _storageClient = new Storage.Client(StorageUrl, GetAuthHeaders());
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
                    if (AuthClient.CurrentSession != null && AuthClient.CurrentSession.AccessToken != null)
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
            headers["apiKey"] = SupabaseKey;
            headers["X-Client-Info"] = Util.GetAssemblyVersion();

            // In Regard To: https://github.com/supabase/supabase-csharp/issues/5
            if (options.Headers.ContainsKey("Authorization"))
            {
                headers["Authorization"] = options.Headers["Authorization"];
            }
            else
            {
                var bearer = AuthClient?.CurrentSession?.AccessToken != null ? AuthClient.CurrentSession.AccessToken : SupabaseKey;
                headers["Authorization"] = $"Bearer {bearer}";
            }

            return headers;
        }
    }
}

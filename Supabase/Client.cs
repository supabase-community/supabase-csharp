using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using Storage.Interfaces;
using Supabase.Core;
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

        /// <summary>
        /// Supabase Auth allows you to create and manage user sessions for access to data that is secured by access policies.
        /// </summary>
        public IGotrueClient<User, Session> Auth
        {
            get
            {
                return _auth;
            }
            set
            {
                // Remove existing internal state listener (if applicable)
                if (_auth != null)
                    _auth.StateChanged -= Auth_StateChanged;

                _auth = value;
                _auth.StateChanged += Auth_StateChanged;
            }
        }
        private IGotrueClient<User, Session> _auth;

        /// <summary>
        /// Supabase Realtime allows for realtime feedback on database changes.
        /// </summary>
        public IRealtimeClient<Socket, Channel> Realtime
        {
            get
            {
                return _realtime;
            }
            set
            {
                // Disconnect from previous socket (if applicable)
                if (_realtime != null)
                    _realtime.Disconnect();

                _realtime = value;
            }
        }
        private IRealtimeClient<Socket, Channel> _realtime;

        /// <summary>
        /// Supabase Edge functions allow you to deploy and invoke edge functions.
        /// </summary>
        public IFunctionsClient Functions
        {
            get => _functions;
            set => _functions = value;
        }
        private IFunctionsClient _functions;

        /// <summary>
        /// Supabase Postgrest allows for strongly typed REST interactions with the your database.
        /// </summary>
        public IPostgrestClient Postgrest
        {
            get => _postgrest;
            set => _postgrest = value;
        }
        private IPostgrestClient _postgrest;

        /// <summary>
        /// Supabase Storage allows you to manage user-generated content, such as photos or videos.
        /// </summary>
        public IStorageClient<Bucket, FileObject> Storage
        {
            get => _storage;
            set => _storage = value;
        }
        private IStorageClient<Bucket, FileObject> _storage;

        private string? supabaseKey;
        private SupabaseOptions options;

        /// <summary>
        /// Constructor supplied for dependency injection support.
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="realtime"></param>
        /// <param name="functions"></param>
        /// <param name="postgrest"></param>
        /// <param name="storage"></param>
        /// <param name="options"></param>
        public Client(IGotrueClient<User, Session> auth, IRealtimeClient<Socket, Channel> realtime, IFunctionsClient functions, IPostgrestClient postgrest, IStorageClient<Bucket, FileObject> storage, SupabaseOptions options)
        {
            _auth = auth;
            _realtime = realtime;
            _functions = functions;
            _postgrest = postgrest;
            _storage = storage;
            this.options = options;
        }

        /// <summary>
        /// Creates a new Supabase Client.
        /// </summary>
        /// <param name="supabaseUrl"></param>
        /// <param name="supabaseKey"></param>
        /// <param name="options"></param>
        public Client(string supabaseUrl, string? supabaseKey, SupabaseOptions? options = null)
        {
            this.supabaseKey = supabaseKey;

            options ??= new SupabaseOptions();
            this.options = options;

            var authUrl = string.Format(options.AuthUrlFormat, supabaseUrl);
            var restUrl = string.Format(options.RestUrlFormat, supabaseUrl);
            var realtimeUrl = string.Format(options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
            var storageUrl = string.Format(options.StorageUrlFormat, supabaseUrl);
            var schema = options.Schema;

            // See: https://github.com/supabase/supabase-js/blob/09065a65f171bc28a9fd7b831af2c24e5f1a380b/src/SupabaseClient.ts#L77-L83
            var isPlatform = new Regex(@"(supabase\.co)|(supabase\.in)").Match(supabaseUrl);

            string? functionsUrl;
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
            var gotrueOptions = new Gotrue.ClientOptions<Session>
            {
                Url = authUrl,
                AutoRefreshToken = options.AutoRefreshToken,
                PersistSession = options.PersistSession,
                SessionDestroyer = options.SessionHandler.SessionDestroyer,
                SessionPersistor = options.SessionHandler.SessionPersistor,
                SessionRetriever = options.SessionHandler.SessionRetriever<Session>
            };

            _auth = new Gotrue.Client(gotrueOptions);
            _auth.StateChanged += Auth_StateChanged;
            _auth.GetHeaders = () => GetAuthHeaders();


            // Init Realtime

            var realtimeOptions = new Realtime.ClientOptions
            {
                Parameters = { ApiKey = this.supabaseKey }
            };

            _realtime = new Realtime.Client(realtimeUrl, realtimeOptions);

            _postgrest = new Postgrest.Client(restUrl, new Postgrest.ClientOptions { Schema = schema });
            _postgrest.GetHeaders = () => GetAuthHeaders();

            _functions = new Functions.Client(functionsUrl);
            _functions.GetHeaders = () => GetAuthHeaders();

            _storage = new Storage.Client(storageUrl, GetAuthHeaders());
            _storage.GetHeaders = () => GetAuthHeaders();
        }


        /// <summary>
        /// Attempts to retrieve the session from Gotrue (set in <see cref="SupabaseOptions"/>) and connects to realtime (if `options.AutoConnectRealtime` is set)
        /// </summary>
        public async Task<ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject>> InitializeAsync()
        {
            await Auth.RetrieveSessionAsync();

            if (options.AutoConnectRealtime)
            {
                await Realtime.ConnectAsync();
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
                    if (Auth.CurrentSession?.AccessToken != null)
                        Realtime.SetAuth(Auth.CurrentSession.AccessToken);
                    break;

                // Remove Realtime Subscriptions on Auth Signout.
                case AuthState.SignedOut:
                    foreach (var subscription in Realtime.Subscriptions.Values)
                        subscription.Unsubscribe();
                    Realtime.Disconnect();
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
        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters) => _postgrest.Rpc(procedureName, parameters);

        internal Dictionary<string, string> GetAuthHeaders()
        {
            var headers = new Dictionary<string, string>();

            headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

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
                var bearer = Auth.CurrentSession?.AccessToken != null ? Auth.CurrentSession.AccessToken : supabaseKey;
                headers["Authorization"] = $"Bearer {bearer}";
            }

            return headers;
        }
    }
}

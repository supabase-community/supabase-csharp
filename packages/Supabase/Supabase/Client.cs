using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Supabase.Core;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Interfaces;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Storage;
using Supabase.Storage.Interfaces;
using static Supabase.Gotrue.Constants;

namespace Supabase;

/// <summary>
/// A class representing the Supabase Client, coordinating between all child APIs
/// </summary>
public class Client : ISupabaseClient<User, Session, RealtimeSocket, RealtimeChannel, Bucket, FileObject>
{
    /// <summary>
    /// Supabase Auth allows you to create and manage user sessions for access to data that is secured by access policies.
    /// </summary>
    public IGotrueClient<User, Session> Auth
    {
        get => this.auth;
        set
        {
            // Remove existing internal state listener (if applicable)
            this.auth.RemoveStateChangedListener(this.Auth_StateChanged);
            this.auth = value;
            this.auth.AddStateChangedListener(this.Auth_StateChanged);
        }
    }

    private IGotrueClient<User, Session> auth;

    /// <summary>
    /// Returns a Stateless Gotrue Admin client given a service_key JWT. This should really only be accessed from a
    /// server environment where a private service_key would remain secure.
    /// </summary>
    /// <param name="serviceKey"></param>
    /// <returns></returns>
    public IGotrueAdminClient<User> AdminAuth(string serviceKey) =>
        new AdminClient(serviceKey, new Gotrue.ClientOptions
        {
            Url = string.Format(this.options.AuthUrlFormat, this.supabaseUrl),
            AutoRefreshToken = this.options.AutoRefreshToken,
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy,
            Retry = this.options.GotrueRetry
        })
        {
            GetHeaders = this.GetAuthHeaders,
        };

    /// <summary>
    /// Supabase Realtime allows for realtime feedback on database changes.
    /// </summary>
    public IRealtimeClient<RealtimeSocket, RealtimeChannel> Realtime
    {
        get => this.realtime;
        set
        {
            // Disconnect from previous RealtimeSocket (if applicable)
            this.realtime.Disconnect();
            this.realtime = value;
        }
    }

    private IRealtimeClient<RealtimeSocket, RealtimeChannel> realtime;

    /// <summary>
    /// Supabase Edge functions allow you to deploy and invoke edge functions.
    /// </summary>
    public IFunctionsClient Functions
    {
        get => this.functions;
        set => this.functions = value;
    }

    private IFunctionsClient functions;

    /// <summary>
    /// Supabase Postgrest allows for strongly typed REST interactions with your database.
    /// </summary>
    public IPostgrestClient Postgrest
    {
        get => this.postgrest;
        set => this.postgrest = value;
    }

    private IPostgrestClient postgrest;

    /// <summary>
    /// Supabase Storage allows you to manage user-generated content, such as photos or videos.
    /// </summary>
    public IStorageClient<Bucket, FileObject> Storage
    {
        get => this.storage;
        set => this.storage = value;
    }

    private IStorageClient<Bucket, FileObject> storage;

    private readonly string? supabaseUrl;
    private readonly string? supabaseKey;
    private readonly SupabaseOptions options;

    /// <summary>
    /// Constructor supplied for dependency injection support.
    /// </summary>
    /// <param name="auth"></param>
    /// <param name="realtime"></param>
    /// <param name="functions"></param>
    /// <param name="postgrest"></param>
    /// <param name="storage"></param>
    /// <param name="options"></param>
    public Client(IGotrueClient<User, Session> auth, IRealtimeClient<RealtimeSocket, RealtimeChannel> realtime,
        IFunctionsClient functions, IPostgrestClient postgrest, IStorageClient<Bucket, FileObject> storage,
        SupabaseOptions options)
    {
        this.auth = auth;
        this.realtime = realtime;
        this.functions = functions;
        this.postgrest = postgrest;
        this.storage = storage;
        this.options = options;
        this.realtime.Options.PostgrestClient = this.postgrest;
    }

    /// <summary>
    /// Creates a new Supabase Client.
    /// </summary>
    /// <param name="supabaseUrl"></param>
    /// <param name="supabaseKey"></param>
    /// <param name="options"></param>
    public Client(string supabaseUrl, string? supabaseKey, SupabaseOptions? options = null)
    {
        this.supabaseUrl = supabaseUrl;
        this.supabaseKey = supabaseKey;
        this.options = options ?? new SupabaseOptions();

        var authUrl = string.Format(this.options.AuthUrlFormat, supabaseUrl);
        var restUrl = string.Format(this.options.RestUrlFormat, supabaseUrl);
        var realtimeUrl = string.Format(this.options.RealtimeUrlFormat, supabaseUrl).Replace("http", "ws");
        var storageUrl = string.Format(this.options.StorageUrlFormat, supabaseUrl);
        var schema = this.options.Schema;

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
            functionsUrl = string.Format(this.options.FunctionsUrlFormat, supabaseUrl);
        }

        // Init Auth
        var gotrueOptions = new Gotrue.ClientOptions
        {
            Url = authUrl,
            AutoRefreshToken = this.options.AutoRefreshToken,
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy,
            Retry = this.options.GotrueRetry
        };
        this.auth = new Gotrue.Client(gotrueOptions);
        this.auth.SetPersistence(this.options.SessionHandler);
        this.auth.AddStateChangedListener(this.Auth_StateChanged);
        this.auth.GetHeaders = this.GetAuthHeaders;
        this.postgrest = new Postgrest.Client(restUrl, new Postgrest.ClientOptions
        {
            Schema = schema,
            Retry = this.options.PostgrestRetry,
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy
        });
        this.postgrest.GetHeaders = this.GetAuthHeaders;

        // Init Realtime

        var realtimeOptions = new Realtime.ClientOptions
        {
            Parameters = { ApiKey = this.supabaseKey },
            PostgrestClient = this.postgrest,
            WebSocketFactory = this.options.WebSocketFactory
        };
        this.realtime = new Realtime.Client(realtimeUrl, realtimeOptions);
        this.realtime.GetHeaders = this.GetAuthHeaders;
        this.functions = new Functions.Client(functionsUrl, options: new Functions.ClientOptions
        {
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy,
            Retry = this.options.FunctionsRetry
        });
        this.functions.GetHeaders = this.GetAuthHeaders;
        this.storage = new Storage.Client(storageUrl, this.options.StorageClientOptions);
        this.storage.GetHeaders = this.GetAuthHeaders;
    }


    /// <summary>
    /// Attempts to retrieve the session from Gotrue (set in <see cref="SupabaseOptions"/>) and connects to realtime (if `options.AutoConnectRealtime` is set)
    /// </summary>
    public async Task<ISupabaseClient<User, Session, RealtimeSocket, RealtimeChannel, Bucket, FileObject>>
        InitializeAsync()
    {
        await this.Auth.RetrieveSessionAsync();

        if (this.options.AutoConnectRealtime)
            await this.Realtime.ConnectAsync();

        return this;
    }

    private void Auth_StateChanged(object sender, AuthState e)
    {
        switch (e)
        {
            // Pass new Auth down to Realtime
            // Ref: https://github.com/supabase-community/supabase-csharp/issues/12
            case AuthState.SignedIn:
            case AuthState.TokenRefreshed:
            case AuthState.UserUpdated:
                if (this.Auth.CurrentSession?.AccessToken != null)
                    this.Realtime.SetAuth(this.Auth.CurrentSession.AccessToken);
                break;
            // Remove Realtime Subscriptions on Auth Sign-out.
            case AuthState.SignedOut:
                this.Realtime.Subscriptions.Values?.ToList().ForEach(subscription => subscription.Unsubscribe());
                break;
            case AuthState.PasswordRecovery:
            case AuthState.Shutdown: break;
            case AuthState.MfaChallengeVerified:
            default: throw new ArgumentOutOfRangeException(nameof(e), e, null);
        }
    }

    /// <summary>
    /// Gets the Postgrest client to prepare for a query.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public ISupabaseTable<TModel, RealtimeChannel> From<TModel>() where TModel : BaseModel, new() =>
        new SupabaseTable<TModel>(this.Postgrest, this.Realtime);

    /// <inheritdoc />
    public Task<BaseResponse> Rpc(string procedureName, object? parameters) => this.postgrest.Rpc(procedureName, parameters);

    /// <inheritdoc />
    public Task<TModeledResponse?> Rpc<TModeledResponse>(string procedureName, object? parameters) => this.postgrest.Rpc<TModeledResponse>(procedureName, parameters);

    /// <summary>
    /// Produces a dictionary of Headers that will be supplied to child clients.
    ///</summary>
    internal Dictionary<string, string> GetAuthHeaders()
    {
        var headers = CaseInsensitiveHeaders();
        headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

        if (this.supabaseKey != null)
            headers["apiKey"] = this.supabaseKey;

        // In Regard To: https://github.com/supabase/supabase-csharp/issues/5
        if (this.options.Headers.TryGetValue("Authorization", out var header))
        {
            headers["Authorization"] = header;
        }
        else
        {
            var bearer = this.Auth.CurrentSession?.AccessToken ?? this.supabaseKey;
            headers["Authorization"] = $"Bearer {bearer}";
        }

        // Add supplied headers from `ClientOptions` by developer
        foreach (var kvp in this.options.Headers)
            headers[kvp.Key] = kvp.Value;

        return headers;
    }

    private static Dictionary<string, string> CaseInsensitiveHeaders() => new(StringComparer.OrdinalIgnoreCase);
}

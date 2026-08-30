#region

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Supabase.Core.Diagnostics;
using Supabase.Core.Http;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue.Mfa;
using static Supabase.Gotrue.Constants;
using static Supabase.Gotrue.Constants.AuthState;
using static Supabase.Gotrue.Exceptions.FailureHint.Reason;

#endregion

namespace Supabase.Gotrue;

/// <inheritdoc />
public class Client : IGotrueClient<User, Session>
{
    /// <summary>
    ///     The underlying API requests object that sends the requests
    /// </summary>
    private readonly IGotrueApi<User, Session> api;

    /// <summary>
    ///     Handlers for notifications of state changes.
    /// </summary>
    private readonly List<IGotrueClient<User, Session>.AuthEventHandler> authEventHandlers =
        new List<IGotrueClient<User, Session>.AuthEventHandler>();

    /// <summary>
    ///     Gets notifications if there is a failure not visible by exceptions (e.g. background thread refresh failure)
    /// </summary>
#pragma warning disable CS0618 // internal plumbing for the obsolete debug surface, removed together in v8
    private DebugNotification? debugNotification;
#pragma warning restore CS0618

    /// <summary>
    ///     Object called to persist the session (e.g. filesystem or cookie)
    /// </summary>
    private IGotruePersistenceListener<Session>? sessionPersistence;

    /// <summary>
    ///     Guards <see cref="refreshInFlight" />.
    /// </summary>
    private readonly object refreshGate = new();

    /// <summary>
    ///     The running token refresh, shared by concurrent callers. A completed attempt is replaced, never reused.
    /// </summary>
    private Task? refreshInFlight;

    /// <summary>
    ///     The refresh token <see cref="refreshInFlight" /> was started for. A caller holding a different one
    ///     belongs to another session, so it starts its own attempt.
    /// </summary>
    private string? refreshInFlightToken;

    /// <summary>
    ///     Initializes the GoTrue stateful client.
    ///     You will likely want to at least specify a
    ///     <see>
    ///         <cref>ClientOptions.Url</cref>
    ///     </see>
    ///     Sessions are not automatically retrieved when this object is created.
    ///     If you want to load the session from your persistence store,
    ///     <see>
    ///         <cref>GotrueSessionPersistence</cref>
    ///     </see>
    ///     .
    ///     If you want to load/refresh the session,
    ///     <see>
    ///         <cref>RetrieveSessionAsync</cref>
    ///     </see>
    ///     .
    ///     For a typical client application, you'll want to load the session from persistence
    ///     and then refresh it. If your application is listening for session changes, you'll
    ///     get two SignIn notifications if the persisted session is valid - one for the
    ///     session loaded from disk, and a second on a successful session refresh.
    ///     <remarks></remarks>
    ///     <example>
    ///         var client = new Supabase.Gotrue.Client(options);
    ///         client.LoadSession();
    ///         await client.RetrieveSessionAsync();
    ///     </example>
    /// </summary>
    /// <param name="options"></param>
    public Client(ClientOptions? options = null)
    {
        options ??= new ClientOptions();
        this.Options = options;
        this.api = new Api(options.Url, options.Headers, options.HttpClient ?? (options.Proxy != null ? DefaultHttpClientFactory.Create(proxy: options.Proxy) : null), options.Retry);
        if (options.AutoRefreshToken)
        {
            this.TokenRefresh = new TokenRefresh(this);
            this.authEventHandlers.Add(this.TokenRefresh.ManageAutoRefresh);
        }
    }

    /// <summary>
    ///     Get the TokenRefresh object, if it exists
    /// </summary>
    public TokenRefresh? TokenRefresh { get; }

    /// <inheritdoc />
    public void SetPersistence(IGotrueSessionPersistence<Session> persistence)
    {
        if (this.sessionPersistence != null)
        {
            this.authEventHandlers.Remove(this.sessionPersistence.EventHandler);
        }
        this.sessionPersistence = new PersistenceListener(persistence);
        this.authEventHandlers.Add(this.sessionPersistence.EventHandler);
    }

    /// <inheritdoc />
    public ClientOptions Options { get; }

    /// <inheritdoc />
    public Task<User?> GetUser(string jwt) => this.api.GetUser(jwt);

    /// <inheritdoc />
    public void NotifyAuthStateChange(AuthState stateChanged)
    {
        foreach (var handler in this.authEventHandlers)
        {
            try
            {
                handler.Invoke(this, stateChanged);
            }
            catch (Exception e)
            {
                this.debugNotification?.Log("Auth State Change Handler Failure", e);
            }
        }
    }

    /// <inheritdoc />
    public User? CurrentUser => this.CurrentSession?.User;

    /// <inheritdoc />
    public void AddStateChangedListener(IGotrueClient<User, Session>.AuthEventHandler authEventHandler)
    {
        if (this.authEventHandlers.Contains(authEventHandler))
        {
            return;
        }
        this.authEventHandlers.Add(authEventHandler);
    }

    /// <inheritdoc />
    public void RemoveStateChangedListener(IGotrueClient<User, Session>.AuthEventHandler authEventHandler)
    {
        if (!this.authEventHandlers.Contains(authEventHandler))
        {
            return;
        }
        this.authEventHandlers.Remove(authEventHandler);
    }

    /// <inheritdoc />
    public void ClearStateChangedListeners() => this.authEventHandlers.Clear();

    /// <inheritdoc />
    public bool Online { get; set; } = true;

    /// <inheritdoc />
    public Session? CurrentSession { get; private set; }

    /// <inheritdoc />
    public Task<Session?> SignUp(string email, string password, SignUpOptions? options = null) =>
        this.SignUp(SignUpType.Email, email, password, options);

    /// <inheritdoc />
    public async Task<Session?> SignUp(SignUpType type, string identifier, string password,
        SignUpOptions? options = null)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignUp);
        activity?.SetTag(GotrueInstrumentation.Tags.SignUpType, type.ToString());
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var session = type switch
        {
            SignUpType.Email => await this.api.SignUpWithEmail(identifier, password, options),
            SignUpType.Phone => await this.api.SignUpWithPhone(identifier, password, options),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
        if (session?.User?.IsConfirmed == true || session?.User != null && this.Options.AllowUnconfirmedUserSessions)
        {
            this.UpdateSession(session);
            this.NotifyAuthStateChange(SignedIn);
            return this.CurrentSession;
        }
        return session;
    }

    /// <inheritdoc />
    public async Task<bool> SignIn(string email, SignInOptions? options = null)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SendMagicLink);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        await this.api.SendMagicLinkEmail(email, options);
        return true;
    }

    /// <inheritdoc />
    public async Task<Session?> SignInWithIdToken(Provider provider, string idToken, string? accessToken = null, string? nonce = null,
        string? captchaToken = null)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignInWithIdToken);
        activity?.SetTag(GotrueInstrumentation.Tags.Provider, provider.ToString());
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var result = await this.api.SignInWithIdToken(provider, idToken, accessToken, nonce, captchaToken);
        this.UpdateSession(result);
        this.NotifyAuthStateChange(SignedIn);
        return result;
    }

    /// <inheritdoc />
    public async Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessEmailOptions options)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignInWithOtp);
        activity?.SetTag(GotrueInstrumentation.Tags.OtpChannel, GotrueInstrumentation.Channels.Email);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        return await this.api.SignInWithOtp(options);
    }

    /// <inheritdoc />
    public async Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessPhoneOptions options)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignInWithOtp);
        activity?.SetTag(GotrueInstrumentation.Tags.OtpChannel, GotrueInstrumentation.Channels.Phone);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        return await this.api.SignInWithOtp(options);
    }

    /// <inheritdoc />
    public Task<bool> SendMagicLink(string email, SignInOptions? options = null) => this.SignIn(email, options);

    /// <inheritdoc />
    public Task<Session?> SignIn(string email, string password) => this.SignIn(SignInType.Email, email, password);

    /// <inheritdoc />
    public Task<Session?> SignInWithPassword(string email, string password) => this.SignIn(email, password);

    /// <inheritdoc />
    public async Task<Session?> SignIn(SignInType type, string identifierOrToken, string? password = null,
        string? scopes = null)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignIn);
        activity?.SetTag(GotrueInstrumentation.Tags.SignInType, type.ToString());
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        Session? newSession;
        switch (type)
        {
            case SignInType.Email:
                newSession = await this.api.SignInWithEmail(identifierOrToken, password!);
                this.UpdateSession(newSession);
                break;
            case SignInType.Phone:
                if (string.IsNullOrEmpty(password))
                {
                    await this.api.SendMobileOTP(identifierOrToken);
                    return null;
                }
                newSession = await this.api.SignInWithPhone(identifierOrToken, password!);
                this.UpdateSession(newSession);
                break;
            case SignInType.RefreshToken:
                if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
                {
                    throw new GotrueException("Not logged in.", NoSessionFound);
                }
                await this.RefreshToken(this.CurrentSession.AccessToken!, identifierOrToken);
                return this.CurrentSession;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        // Handle case when a user registers and has not confirmed email (and options do not allow for this), return null for session.
        if (newSession?.User?.IsConfirmed != true &&
            (newSession?.User == null || !this.Options.AllowUnconfirmedUserSessions))
        {
            return null;
        }
        this.NotifyAuthStateChange(SignedIn);
        return this.CurrentSession;
    }

    /// <inheritdoc />
    public Task<ProviderAuthState> SignIn(Provider provider, SignInOptions? options = null)
    {
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var providerUri = this.api.GetUriForProvider(provider, options);
        return Task.FromResult(providerUri);
    }

    /// <inheritdoc />
    public Task<SSOResponse?> SignInWithSSO(Guid providerId, SignInWithSSOOptions? options = null)
    {
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        return this.api.SignInWithSSO(providerId, options);
    }

    /// <inheritdoc />
    public Task<SSOResponse?> SignInWithSSO(string domain, SignInWithSSOOptions? options = null)
    {
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        return this.api.SignInWithSSO(domain, options);
    }

    /// <inheritdoc />
    public async Task<Session?> SignInAnonymously(SignInAnonymouslyOptions? options = null)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignInAnonymously);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var newSession = await this.api.SignInAnonymously(options);
        this.UpdateSession(newSession);
        this.NotifyAuthStateChange(SignedIn);
        return this.CurrentSession;
    }

    /// <inheritdoc />
    public async Task<Session?> VerifyOTP(string phone, string token, MobileOtpType type = MobileOtpType.SMS)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.VerifyOtp);
        activity?.SetTag(GotrueInstrumentation.Tags.OtpChannel, GotrueInstrumentation.Channels.Phone);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var session = await this.api.VerifyMobileOTP(phone, token, type);
        if (session?.AccessToken != null)
        {
            this.UpdateSession(session);
            this.NotifyAuthStateChange(SignedIn);
            return session;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<Session?> VerifyOTP(string email, string token, EmailOtpType type = EmailOtpType.MagicLink)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.VerifyOtp);
        activity?.SetTag(GotrueInstrumentation.Tags.OtpChannel, GotrueInstrumentation.Channels.Email);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var session = await this.api.VerifyEmailOTP(email, token, type);
        if (session?.AccessToken != null)
        {
            this.UpdateSession(session);
            this.NotifyAuthStateChange(SignedIn);
            return session;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<Session?> VerifyTokenHash(string tokenHash, EmailOtpType type = EmailOtpType.Email)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.VerifyTokenHash);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        this.DestroySession();
        var session = await this.api.VerifyTokenHash(tokenHash, type);
        if (session?.AccessToken != null)
        {
            this.UpdateSession(session);
            this.NotifyAuthStateChange(SignedIn);
            return session;
        }
        return null;
    }

    /// <inheritdoc />
    public Task<ProviderAuthState> LinkIdentity(Provider provider, SignInOptions options)
    {
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        if (this.CurrentSession == null || this.CurrentUser == null)
        {
            throw new GotrueException("A valid session is required.", NoSessionFound);
        }
        if (options.FlowType != OAuthFlowType.PKCE)
        {
            throw new GotrueException("PKCE flow type is required for this action.", InvalidFlowType);
        }
        return this.api.LinkIdentity(this.CurrentSession.AccessToken!, provider, options);
    }

    /// <inheritdoc />
    public async Task<Session?> LinkIdentityWithIdToken(LinkIdentityWithIdTokenOptions options)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.LinkIdentityWithIdToken);
        activity?.SetTag(GotrueInstrumentation.Tags.Provider, options.Provider.ToString());
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        if (this.CurrentSession == null || this.CurrentUser == null)
        {
            throw new GotrueException("A valid session is required.", NoSessionFound);
        }
        var result = await this.api.LinkIdentityWithIdToken(this.CurrentSession.AccessToken!, options).ConfigureAwait(false);
        if (result?.AccessToken != null)
        {
            this.UpdateSession(result);
            this.NotifyAuthStateChange(SignedIn);
        }
        return result;
    }

    /// <inheritdoc />
    public Task<bool> UnlinkIdentity(UserIdentity userIdentity)
    {
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        if (this.CurrentSession == null || this.CurrentUser == null)
        {
            throw new GotrueException("A valid session is required.", NoSessionFound);
        }
        return this.api.UnlinkIdentity(this.CurrentSession.AccessToken!, userIdentity);
    }

    /// <inheritdoc />
    public async Task SignOut(SignOutScope scope = SignOutScope.Global)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SignOut);
        activity?.SetTag(GotrueInstrumentation.Tags.SignOutScope, scope.ToString());
        if (this.CurrentSession?.AccessToken != null)
        {
            await this.api.SignOut(this.CurrentSession.AccessToken, scope);
        }
        this.UpdateSession(null);
        this.NotifyAuthStateChange(SignedOut);
    }

    /// <inheritdoc />
    public async Task<User?> Update(UserAttributes attributes)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.UpdateUser);
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.");
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        var result = await this.api.UpdateUser(this.CurrentSession.AccessToken!, attributes);
        this.CurrentSession.User = result;
        this.NotifyAuthStateChange(UserUpdated);
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> Reauthenticate()
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        var response = await this.api.Reauthenticate(this.CurrentSession.AccessToken!);
        return response.ResponseMessage?.IsSuccessStatusCode ?? false;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordForEmail(string email)
    {
        var result = await this.api.ResetPasswordForEmail(email);
        result.ResponseMessage?.EnsureSuccessStatusCode();
        return true;
    }

    /// <inheritdoc />
    public async Task<ResetPasswordForEmailState> ResetPasswordForEmail(ResetPasswordForEmailOptions options)
    {
        var state = await this.api.ResetPasswordForEmail(options);
        return state;
    }

    /// <inheritdoc />
    public async Task<Session?> RefreshSession()
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.RefreshSession);
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        await this.RefreshToken();
        var session = this.CurrentSession;
        if (session == null)
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        session.User = await this.api.GetUser(session.AccessToken);
        return session;
    }

    /// <inheritdoc />
    public async Task<Session> SetSession(string accessToken, string refreshToken, bool forceAccessTokenRefresh = false)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.SetSession);
        this.DestroySession();
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            throw new GotrueException("`accessToken` and `refreshToken` cannot be empty.", NoSessionFound);
        }
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Payload;
        if (payload == null || payload.ValidTo == DateTime.MinValue)
        {
            throw new GotrueException("`accessToken`'s payload was of an unknown structure.", NoSessionFound);
        }
        if (payload.ValidTo < DateTime.UtcNow || forceAccessTokenRefresh)
        {
            var result = await this.api.RefreshAccessToken(accessToken, refreshToken);
            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                throw new GotrueException("Could not generate a session given the provided parameters.", NoSessionFound);
            }
            this.CurrentSession = result;
            this.NotifyAuthStateChange(SignedIn);
            return this.CurrentSession;
        }
        var iat = payload.IssuedAt;
        var exp = payload.ValidTo;
        var expiresIn = (long) (exp - iat).TotalSeconds;
        this.CurrentSession = new Session
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "bearer",
            ExpiresIn = expiresIn,
            User = await this.api.GetUser(accessToken),
        };
        this.NotifyAuthStateChange(SignedIn);
        return this.CurrentSession;
    }

    /// <summary>
    ///     Parses a <see cref="Session" /> out of a <see cref="Uri" />'s Query parameters.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="storeSession"></param>
    /// <returns></returns>
    public async Task<Session?> GetSessionFromUrl(Uri uri, bool storeSession = true)
    {
        var query = string.IsNullOrEmpty(uri.Fragment)
            ? HttpUtility.ParseQueryString(uri.Query)
            : HttpUtility.ParseQueryString('?' + uri.Fragment.TrimStart('#'));
        var errorDescription = query.Get("error_description");
        if (!string.IsNullOrEmpty(errorDescription))
        {
            throw new GotrueException(errorDescription, BadSessionUrl);
        }
        var accessToken = query.Get("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new GotrueException("No access_token detected.", BadSessionUrl);
        }
        var expiresIn = query.Get("expires_in");
        if (string.IsNullOrEmpty(expiresIn))
        {
            throw new GotrueException("No expires_in detected.", BadSessionUrl);
        }
        var refreshToken = query.Get("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new GotrueException("No refresh_token detected.", BadSessionUrl);
        }
        var tokenType = query.Get("token_type");
        if (string.IsNullOrEmpty(tokenType))
        {
            throw new GotrueException("No token_type detected.", BadSessionUrl);
        }
        var user = await this.api.GetUser(accessToken);
        var session = new Session
        {
            AccessToken = accessToken,
            ExpiresIn = long.Parse(expiresIn),
            RefreshToken = refreshToken,
            TokenType = tokenType,
            User = user,
        };
        if (storeSession)
        {
            this.UpdateSession(session);
            this.NotifyAuthStateChange(SignedIn);
            if (query.Get("type") == "recovery")
            {
                this.NotifyAuthStateChange(PasswordRecovery);
            }
        }
        return session;
    }

    /// <inheritdoc />
    public async Task<Session?> RetrieveSessionAsync()
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.RetrieveSession);

        // No session, so just return.
        if (this.CurrentSession == null)
        {
            return null;
        }

        // We can't refresh the token offline, so return the session as loaded.
        if (!this.Online)
        {
            return this.CurrentSession;
        }

        // We have a session, and hasn't expired, and we seem to be online. Let's try to refresh it.
        if (this.Options.AutoRefreshToken && this.CurrentSession?.RefreshToken != null)
        {
            try
            {
                await this.RefreshToken();
                return this.CurrentSession;
            }
            catch (GotrueException e) when (e.Reason is InvalidRefreshToken)
            {
                // RefreshToken destroyed the session, unless it was replaced mid-flight.
                activity.SetFailure(e);
                return this.CurrentSession;
            }
            catch (Exception e)
            {
                // Anything else is treated as transient - keep the session so the next refresh can retry.
                // Never log the session itself here - it contains the access and refresh tokens.
                this.debugNotification?.Log($"Failed to refresh token ({e.Message})", e);
                activity.SetFailure(e);
                return this.CurrentSession;
            }
        }
        return this.CurrentSession;
    }

    /// <inheritdoc />
    public async Task<Session?> ExchangeCodeForSession(string codeVerifier, string authCode)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.ExchangeCode);
        var result = await this.api.ExchangeCodeForSession(codeVerifier, authCode);
        if (result != null)
        {
            this.UpdateSession(result);
            this.NotifyAuthStateChange(SignedIn);
            return this.CurrentSession;
        }
        return null;
    }

    /// <summary>
    ///     Headers sent to the API on every request.
    /// </summary>
    public Func<Dictionary<string, string>>? GetHeaders
    {
        get => this.api.GetHeaders;
        set => this.api.GetHeaders = value;
    }

    /// <inheritdoc />
    [Obsolete("The debug listener is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Gotrue\". This member will be removed in v8.")]
    public void AddDebugListener(Action<string, Exception?> listener)
    {
#pragma warning disable CS0618 // internal plumbing for the obsolete debug surface, removed together in v8
        this.debugNotification ??= new DebugNotification();
#pragma warning restore CS0618
        this.debugNotification.AddDebugListener(listener);
    }

    /// <inheritdoc />
    public async Task RefreshToken(string accessToken, string refreshToken)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.RefreshToken);
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            throw new GotrueException("No token provided", NoSessionFound);
        }
        try
        {
            var result = await this.api.RefreshAccessToken(accessToken, refreshToken);
            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                throw new GotrueException("Could not refresh token from provided session.", NoSessionFound);
            }
            this.CurrentSession = result;
            this.NotifyAuthStateChange(TokenRefreshed);
        }
        catch (GotrueException ex) when (ex.Reason is InvalidRefreshToken)
        {
            activity.SetFailure(ex);
            this.DestroySession();
            this.NotifyAuthStateChange(SignedOut);
            throw;
        }
        catch (Exception ex)
        {
            activity.SetFailure(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RefreshToken()
    {
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        var session = this.CurrentSession;
        if (session == null || string.IsNullOrEmpty(session.AccessToken) || string.IsNullOrEmpty(session.RefreshToken))
        {
            throw new GotrueException("No current session.", NoSessionFound);
        }
        Task attempt;
        // Refresh tokens are single-use, and startup can race the auto-refresh timer here - so callers share the in-flight attempt.
        lock (this.refreshGate)
        {
            if (this.refreshInFlight is not { IsCompleted: false } || this.refreshInFlightToken != session.RefreshToken)
            {
                this.refreshInFlightToken = session.RefreshToken;
                this.refreshInFlight = this.RefreshCurrentSession(session);
            }
            attempt = this.refreshInFlight;
        }
        await attempt;
    }

    private async Task RefreshCurrentSession(Session session)
    {
        using var activity = GotrueInstrumentation.Source.StartActivity(GotrueInstrumentation.Spans.RefreshToken);
        try
        {
            var result = await this.api.RefreshAccessToken(session.AccessToken!, session.RefreshToken!);
            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                throw new GotrueException("Could not refresh token from provided session.", NoSessionFound);
            }
            // The session was replaced while this call was in flight, so the result is the previous user's.
            if (this.CurrentSession?.RefreshToken != session.RefreshToken)
            {
                return;
            }
            this.CurrentSession = result;
            this.NotifyAuthStateChange(TokenRefreshed);
        }
        catch (GotrueException ex) when (ex.Reason is InvalidRefreshToken)
        {
            activity.SetFailure(ex);
            if (this.CurrentSession?.RefreshToken == session.RefreshToken)
            {
                this.DestroySession();
                this.NotifyAuthStateChange(SignedOut);
            }
            throw;
        }
        catch (Exception ex)
        {
            // The auto-refresh timer swallows this exception, so mark the span failed to keep it in traces.
            activity.SetFailure(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public void LoadSession()
    {
        if (this.sessionPersistence == null)
        {
            return;
        }
        Session? session;
        try
        {
            session = this.sessionPersistence.Persistence.LoadSession();
        }
        catch (Exception e)
        {
            // A store that fails to load (locked file, corrupt payload) must not crash startup.
            this.debugNotification?.Log($"Failed to load the persisted session ({e.Message})", e);
            return;
        }
        // An emptied store clears the session it was holding, but a cold start with nothing on either
        // side is a no-op: firing SignedOut there would destroy the persistence.
        if (session != null || this.CurrentSession != null)
        {
            this.UpdateSession(session);
        }
    }

    /// <inheritdoc />
    public Task<Settings?> Settings()
    {
        if (!this.Online)
        {
            return Task.FromResult<Settings?>(null);
        }
        return this.api.Settings();
    }

    /// <inheritdoc />
    public void Debug(string message, Exception? e = null) => this.debugNotification?.Log(message, e);

    /// <inheritdoc />
    public void Shutdown() => this.NotifyAuthStateChange(AuthState.Shutdown);

    /// <inheritdoc />
    public async Task<MfaEnrollResponse?> Enroll(MfaEnrollParams mfaEnrollParams)
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        return await this.api.Enroll(this.CurrentSession.AccessToken, mfaEnrollParams);
    }

    /// <inheritdoc />
    public async Task<MfaChallengeResponse?> Challenge(MfaChallengeParams mfaChallengeParams)
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        return await this.api.Challenge(this.CurrentSession.AccessToken, mfaChallengeParams);
    }

    /// <inheritdoc />
    public async Task<Session?> Verify(MfaVerifyParams mfaVerifyParams)
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        var result = await this.api.Verify(this.CurrentSession.AccessToken, mfaVerifyParams);
        if (result == null || string.IsNullOrEmpty(result.AccessToken))
        {
            throw new GotrueException("Could not verify MFA.", MfaChallengeUnverified);
        }
        var session = new Session
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            TokenType = "bearer",
            ExpiresIn = result.ExpiresIn,
            User = result.User,
        };
        this.UpdateSession(session);
        this.NotifyAuthStateChange(MfaChallengeVerified);
        return session;
    }

    /// <inheritdoc />
    public async Task<Session?> ChallengeAndVerify(MfaChallengeAndVerifyParams mfaChallengeAndVerifyParams)
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        var challengeResponse = await this.api.Challenge(this.CurrentSession.AccessToken, new MfaChallengeParams
        {
            FactorId = mfaChallengeAndVerifyParams.FactorId,
        });
        if (challengeResponse == null)
        {
            return null;
        }
        var result = await this.api.Verify(this.CurrentSession.AccessToken, new MfaVerifyParams
        {
            FactorId = mfaChallengeAndVerifyParams.FactorId,
            Code = mfaChallengeAndVerifyParams.Code,
            ChallengeId = challengeResponse.Id,
        });
        if (result == null || string.IsNullOrEmpty(result.AccessToken))
        {
            throw new GotrueException("Could not verify MFA.", MfaChallengeUnverified);
        }
        var session = new Session
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            TokenType = "bearer",
            ExpiresIn = result.ExpiresIn,
            User = result.User,
        };
        this.UpdateSession(session);
        this.NotifyAuthStateChange(MfaChallengeVerified);
        return session;
    }

    /// <inheritdoc />
    public async Task<MfaUnenrollResponse?> Unenroll(MfaUnenrollParams mfaUnenrollParams)
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        if (!this.Online)
        {
            throw new GotrueException("Only supported when online", Offline);
        }
        return await this.api.Unenroll(this.CurrentSession.AccessToken, mfaUnenrollParams);
    }

    /// <inheritdoc />
    public Task<MfaListFactorsResponse?> ListFactors()
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        var response = new MfaListFactorsResponse
        {
            All = this.CurrentSession.User!.Factors,
            Totp = this.CurrentSession.User!.Factors?.Where(x => x.FactorType == "totp" && x.Status == "verified").ToList(),
        };
        return Task.FromResult(response);
    }

    public Task<MfaGetAuthenticatorAssuranceLevelResponse?> GetAuthenticatorAssuranceLevel()
    {
        if (this.CurrentSession == null || string.IsNullOrEmpty(this.CurrentSession.AccessToken))
        {
            throw new GotrueException("Not Logged in.", NoSessionFound);
        }
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(this.CurrentSession.AccessToken).Payload;
        if (payload == null || payload.ValidTo == DateTime.MinValue)
        {
            throw new GotrueException("`accessToken`'s payload was of an unknown structure.", NoSessionFound);
        }
        AuthenticatorAssuranceLevel? currentLevel = null;
        if (payload.ContainsKey("aal"))
        {
            currentLevel = Enum.TryParse(payload["aal"].ToString(), out AuthenticatorAssuranceLevel parsedLevel) ? parsedLevel : (AuthenticatorAssuranceLevel?) null;
        }
        var nextLevel = currentLevel;
        var verifiedFactors = this.CurrentSession.User!.Factors?.Where(factor => factor.Status == "verified").ToList() ?? new List<Factor>();
        if (verifiedFactors.Count > 0)
        {
            nextLevel = AuthenticatorAssuranceLevel.aal2;
        }
        var currentAuthenticationMethods = payload.Amr.Select(x => JsonSerializer.Deserialize<AmrEntry>(x, Helpers.SerializerOptions));
        var response = new MfaGetAuthenticatorAssuranceLevelResponse
        {
            CurrentLevel = currentLevel,
            NextLevel = nextLevel,
            CurrentAuthenticationMethods = currentAuthenticationMethods.ToArray(),
        };
        return Task.FromResult(response);
    }

    /// <summary>
    ///     Saves the session
    /// </summary>
    /// <param name="session"></param>
    private void UpdateSession(Session? session)
    {
        if (session == null)
        {
            // The refresh token is a secret; clearing the session must not leave it behind.
            lock (this.refreshGate)
            {
                this.refreshInFlightToken = null;
            }
            this.CurrentSession = null;
            this.NotifyAuthStateChange(SignedOut);
            return;
        }
        var dirty = this.CurrentSession != session;
        this.CurrentSession = session;
        if (dirty)
        {
            this.NotifyAuthStateChange(UserUpdated);
        }
    }

    /// <summary>
    ///     Clears the session
    /// </summary>
    private void DestroySession() => this.UpdateSession(null);
}

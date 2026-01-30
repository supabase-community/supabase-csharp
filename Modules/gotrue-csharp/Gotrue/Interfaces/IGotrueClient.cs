using System;
using System.Threading.Tasks;
using Supabase.Core.Interfaces;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Mfa;
using static Supabase.Gotrue.Constants;

#pragma warning disable CS1591

namespace Supabase.Gotrue.Interfaces
{
	/// <summary>
	/// GoTrue stateful Client.
	///
	/// This class is best used as a long-lived singleton object in your application. You can attach listeners
	/// to be notified of changes to the user log in state, a persistence system for sessions across application
	/// launches, and more. It includes a (optional, on by default) background thread that runs to refresh the
	/// user's session token.
	///
	/// Check out the test suite for examples of use.
	/// </summary>
	/// <example>
	/// var client = new Supabase.Gotrue.Client(options);
	/// var user = await client.SignIn("user@email.com", "fancyPassword");
	/// </example>
	public interface IGotrueClient<TUser, TSession> : IGettableHeaders
		where TUser : User
		where TSession : Session
	{
		/// <summary>
		/// Indicates if the client should be considered online or offline.
		///
		/// In a server environment, this client would likely always be online.
		///
		/// On a mobile client, you will want to pair this with a network implementation
		/// to turn this on and off as the device goes online and offline.
		/// </summary>
		bool Online { get; set; }

		/// <summary>
		/// The current Session as managed by this client. Does not refresh tokens or have any other side effects.
		///
		/// You probably don't want to directly make changes to this object - you'll want to use other methods
		/// on this class to make changes.
		/// </summary>
		TSession? CurrentSession { get; }

		/// <summary>
		/// The currently logged in User. This is a local cache of the current session User.
		/// To persist modifications to the User you'll want to use other methods.
		/// <see cref="Update"/>>
		/// </summary>
		TUser? CurrentUser { get; }

		/// <summary>
		/// The method that is called when there is a user state change.
		/// </summary>
		delegate void AuthEventHandler(IGotrueClient<TUser, TSession> sender, AuthState stateChanged);

		/// <summary>
		/// Sets the persistence implementation for the client (e.g. file system, local storage, etc).
		/// </summary>
		/// <param name="persistence"></param>
		void SetPersistence(IGotrueSessionPersistence<TSession> persistence);

		/// <summary>
		/// Adds a listener to be notified when the user state changes (e.g. the user logs in, logs out,
		/// the token is refreshed, etc).
		///
		/// <see cref="AuthState"/>
		/// </summary>
		/// <param name="authEventHandler"></param>
		void AddStateChangedListener(AuthEventHandler authEventHandler);

		/// <summary>
		/// Removes a specified listener from event state changes.
		/// </summary>
		/// <param name="authEventHandler"></param>
		void RemoveStateChangedListener(AuthEventHandler authEventHandler);

		/// <summary>
		/// Clears all of the listeners from receiving event state changes.
		///
		/// WARNING: The persistence handler and refresh token thread are installed as state change
		/// listeners. Clearing the listeners will also delete these handlers.
		/// </summary>
		void ClearStateChangedListeners();

		/// <summary>
		/// Notifies all listeners that the current user auth state has changed.
		///
		/// This is mainly used internally to fire notifications - most client applications won't need this.
		/// </summary>
		/// <param name="stateChanged"></param>
		void NotifyAuthStateChange(AuthState stateChanged);


		/// <summary>
		/// Converts a URL to a session. For client apps, this probably requires setting up URL handlers.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="storeSession"></param>
		/// <returns></returns>
		Task<TSession?> GetSessionFromUrl(Uri uri, bool storeSession = true);

		/// <summary>
		/// Refreshes the currently logged in User's Session.
		/// </summary>
		/// <returns></returns>
		Task<TSession?> RefreshSession();

		/// <summary>
		/// Sends a reset request to an email address.
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		Task<bool> ResetPasswordForEmail(string email);

		/// <summary>
		/// Sends a password reset request to an email address.
		///
		/// Supports the PKCE Flow (the `verifier` from <see cref="ResetPasswordForEmailState"/> will be combined with <see cref="ExchangeCodeForSession"/> in response)
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		Task<ResetPasswordForEmailState> ResetPasswordForEmail(ResetPasswordForEmailOptions options);

		/// <summary>
		/// Typically called as part of the startup process for the client.
		///
		/// This will take the currently loaded session (e.g. from a persistence implementation) and
		/// if possible attempt to refresh it. If the loaded session is expired or invalid, it will
		/// log the user out.
		/// </summary>
		/// <returns></returns>
		Task<TSession?> RetrieveSessionAsync();

		/// <summary>
		/// Sends a Magic email login link to the specified email.
		///
		/// Most of the interesting configuration for this flow is done in the
		/// Supabase/GoTrue admin panel.
		///
		/// </summary>
		/// <param name="email"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		Task<bool> SendMagicLink(string email, SignInOptions? options = null);

		/// <summary>
		/// Sets a new session given a user's access token and their refresh token.
		///
		/// 1. Will destroy the current session (if existing)
		/// 2. Raise a <see cref="AuthState.SignedOut"/> event.
		/// 3. Decode token
		///	  3a. If expired (or bool <paramref name="forceAccessTokenRefresh"></paramref> set), force an access token refresh.
		///   3b. If not expired, set the <see cref="CurrentSession"/> and retrieve <see cref="CurrentUser"/> from the server using the <paramref name="accessToken"/>.
		/// 4. Raise a `<see cref="AuthState.SignedIn"/> event if successful.
		/// </summary>
		/// <param name="accessToken"></param>
		/// <param name="refreshToken"></param>
		/// <param name="forceAccessTokenRefresh"></param>
		/// <returns></returns>
		/// <exception cref="GotrueException">Raised when token combination is invalid.</exception>
		Task<TSession> SetSession(string accessToken, string refreshToken, bool forceAccessTokenRefresh = false);

		/// <summary>
		/// Log in an existing user, or login via a third-party provider.
		/// </summary>
		/// <param name="type">Type of Credentials being passed</param>
		/// <param name="identifierOrToken">An email, phone, or RefreshToken</param>
		/// <param name="password">Password to account (optional if `RefreshToken`)</param>
		/// <param name="scopes">A space-separated list of scopes granted to the OAuth application.</param>
		/// <returns></returns>
		Task<TSession?> SignIn(SignInType type, string identifierOrToken, string? password = null, string? scopes = null);

		/// <summary>
		/// Sends a magic link login email to the specified email.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="options"></param>
		Task<bool> SignIn(string email, SignInOptions? options = null);

		/// <summary>
		/// Signs in a User.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		Task<TSession?> SignIn(string email, string password);

		/// <summary>
		/// Log in a user using magiclink or a one-time password (OTP).
		///
		/// If the `{{ .ConfirmationURL }}` variable is specified in the email template, a magiclink will be sent.
		/// If the `{{ .Token }}` variable is specified in the email template, an OTP will be sent.
		/// If you're using phone sign-ins, only an OTP will be sent. You won't be able to send a magiclink for phone sign-ins.
		///
		/// Be aware that you may get back an error message that will not distinguish
		/// between the cases where the account does not exist or, that the account
		/// can only be accessed via social login.
		///
		/// Do note that you will need to configure a Whatsapp sender on Twilio
		/// if you are using phone sign in with the 'whatsapp' channel. The whatsapp
		/// channel is not supported on other providers at this time.
		/// </summary>
		/// <remarks>Calling this method will wipe out the current session (if any)</remarks>
		/// <param name="options"></param>
		/// <returns></returns>
		Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessEmailOptions options);

		/// <summary>
		/// Log in a user using magiclink or a one-time password (OTP).
		///
		/// If the `{{ .ConfirmationURL }}` variable is specified in the email template, a magiclink will be sent.
		/// If the `{{ .Token }}` variable is specified in the email template, an OTP will be sent.
		/// If you're using phone sign-ins, only an OTP will be sent. You won't be able to send a magiclink for phone sign-ins.
		///
		/// Be aware that you may get back an error message that will not distinguish
		/// between the cases where the account does not exist or, that the account
		/// can only be accessed via social login.
		///
		/// Do note that you will need to configure a Whatsapp sender on Twilio
		/// if you are using phone sign in with the 'whatsapp' channel. The whatsapp
		/// channel is not supported on other providers at this time.
		/// </summary>
		/// <remarks>Calling this method will wipe out the current session (if any)</remarks>
		/// <param name="options"></param>
		/// <returns></returns>
		Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessPhoneOptions options);

		/// <summary>
		/// Log in an existing user with an email and password or phone and password.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		Task<TSession?> SignInWithPassword(string email, string password);

		/// <summary>
		/// Retrieves a <see cref="ProviderAuthState"/> to redirect to for signing in with a <see cref="Provider"/>.
		///
		/// This will likely be paired with a PKCE flow (set in SignInOptions) - after redirecting the
		/// user to the flow, you should pair with <see cref="ExchangeCodeForSession(string, string)"/>
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		Task<ProviderAuthState> SignIn(Provider provider, SignInOptions? options = null);

		/// <summary>
		/// Allows signing in with an ID token issued by certain supported providers.
		/// The [idToken] is verified for validity and a new session is established.
		/// This method of signing in only supports [Provider.Google] or [Provider.Apple].
		/// </summary>
		/// <param name="provider">Provider name or OIDC `iss` value identifying which provider should be used to verify the provided token. Supported names: `google`, `apple`, `azure`, `facebook`</param>
		/// <param name="idToken">OIDC ID token issued by the specified provider. The `iss` claim in the ID token must match the supplied provider. Some ID tokens contain an `at_hash` which require that you provide an `access_token` value to be accepted properly. If the token contains a `nonce` claim you must supply the nonce used to obtain the ID token.</param>
		/// <param name="accessToken">If the ID token contains an `at_hash` claim, then the hash of this value is compared to the value in the ID token.</param>
		/// <param name="nonce">If the ID token contains a `nonce` claim, then the hash of this value is compared to the value in the ID token.</param>
		/// <param name="captchaToken">Verification token received when the user completes the captcha on the site.</param>
		/// <remarks>Calling this method will eliminate the current session (if any).</remarks>
		/// <exception>
		///     <cref>InvalidProviderException</cref>
		/// </exception>
		Task<TSession?> SignInWithIdToken(Provider provider, string idToken, string? accessToken = null, string? nonce = null, string? captchaToken = null);

		/// <summary>
		/// Creates a new anonymous user.
		/// </summary>
		/// <param name="options"></param>
		/// <returns>A session where the is_anonymous claim in the access token JWT set to true</returns>
		Task<TSession?> SignInAnonymously(SignInAnonymouslyOptions? options = null);

		/// <summary>
		/// Sign in using single sign on (SSO) as supported by supabase
		/// To use SSO you need to first set up the providers using the supabase CLI
		/// please follow the guide found here: https://supabase.com/docs/guides/auth/enterprise-sso/auth-sso-saml
		/// </summary>
		/// <param name="providerId">The guid of the provider you wish to use, obtained from running supabase sso list from the CLI</param>
		/// <param name="options">The redirect uri and captcha token, if any</param>
		/// <returns>The Uri returned from supabase auth that a user can use to sign in to their given SSO provider (okta, microsoft entra, gsuite ect...)</returns>
		Task<SSOResponse?> SignInWithSSO(Guid providerId, SignInWithSSOOptions? options = null);

		/// <summary>
		/// Sign in using single sign on (SSO) as supported by supabase
		/// To use SSO you need to first set up the providers using the supabase CLI
		/// please follow the guide found here: https://supabase.com/docs/guides/auth/enterprise-sso/auth-sso-saml
		/// </summary>
		/// <param name="domain">
		/// Your organizations email domain to use for sign in, this domain needs to already be registered with supabase by running the CLI commands
		/// Example: `google.com`
		/// </param>
		/// <param name="options">The redirect uri and captcha token, if any</param>
		/// <returns>The Uri returned from supabase auth that a user can use to sign in to their given SSO provider (okta, microsoft entra, gsuite ect...)</returns>
		Task<SSOResponse?> SignInWithSSO(string domain, SignInWithSSOOptions? options = null);

		/// <summary>
		/// Logs in an existing user via a third-party provider.
		/// </summary>
		/// <param name="codeVerifier"></param>
		/// <param name="authCode"></param>
		Task<TSession?> ExchangeCodeForSession(string codeVerifier, string authCode);

		/// <summary>
		/// Signs up a user
		/// </summary>
		/// <remarks>
		/// Calling this method will log out the current user session (if any).
		///
		/// By default, the user needs to verify their email address before logging in. To turn this off, disable confirm email in your project.
		/// Confirm email determines if users need to confirm their email address after signing up.
		///     - If Confirm email is enabled, a user is returned but session is null.
		///     - If Confirm email is disabled, both a user and a session are returned.
		/// When the user confirms their email address, they are redirected to the SITE_URL by default. You can modify your SITE_URL or add additional redirect URLs in your project.
		/// If signUp() is called for an existing confirmed user:
		///     - If Confirm email is enabled in your project, an obfuscated/fake user object is returned.
		///     - If Confirm email is disabled, the error message, User already registered is returned.
		/// To fetch the currently logged-in user, refer to <see cref="User"/>.
		/// </remarks>
		/// <param name="type"></param>
		/// <param name="identifier"></param>
		/// <param name="password"></param>
		/// <param name="options">Object containing redirectTo and optional user metadata (data)</param>
		/// <returns></returns>
		Task<TSession?> SignUp(SignUpType type, string identifier, string password, SignUpOptions? options = null);

		/// <summary>
		/// Signs up a user by email address.
		/// </summary>
		/// <remarks>
		/// By default, the user needs to verify their email address before logging in. To turn this off, disable Confirm email in your project.
		/// Confirm email determines if users need to confirm their email address after signing up.
		///     - If Confirm email is enabled, a user is returned but session is null.
		///     - If Confirm email is disabled, both a user and a session are returned.
		/// When the user confirms their email address, they are redirected to the SITE_URL by default. You can modify your SITE_URL or
		/// add additional redirect URLs in your project.
		/// If signUp() is called for an existing confirmed user:
		///     - If Confirm email is enabled in your project, an obfuscated/fake user object is returned.
		///     - If Confirm email is disabled, the error message, User already registered is returned.
		/// To fetch the currently logged-in user, refer to <see>
		///     <cref>User</cref>
		/// </see>.
		/// </remarks>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <param name="options">Object containing redirectTo and optional user metadata (data)</param>
		/// <returns></returns>
		Task<TSession?> SignUp(string email, string password, SignUpOptions? options = null);

		/// <summary>
		/// Used for re-authenticating a user in password changes.
		///
		/// See: https://github.com/supabase/gotrue#get-reauthenticate
		/// </summary>
		/// <returns></returns>
		/// <exception cref="GotrueException"></exception>
		Task<bool> Reauthenticate();

		/// <summary>
		/// Signs out and invalidates all sessions for a user.
		/// </summary>
		/// <param name="scope">Determines which sessions should be invalidated. By default, it will invalidate all session for a user</param>
		/// <returns></returns>
		Task SignOut(SignOutScope scope = SignOutScope.Global);

		/// <summary>
		/// Updates a User.
		/// </summary>
		/// <param name="attributes"></param>
		/// <returns></returns>
		Task<TUser?> Update(UserAttributes attributes);

		/// <summary>
		/// Log in a user given a User supplied OTP received via mobile.
		/// </summary>
		/// <param name="phone">The user's phone number.</param>
		/// <param name="token">Token sent to the user's phone.</param>
		/// <param name="type">SMS or phone change</param>
		/// <returns></returns>
		Task<TSession?> VerifyOTP(string phone, string token, MobileOtpType type = MobileOtpType.SMS);

		/// <summary>
		/// Log in a user give a user supplied OTP received via email.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="token"></param>
		/// <param name="type">Defaults to MagicLink</param>
		/// <returns></returns>
		Task<TSession?> VerifyOTP(string email, string token, EmailOtpType type = EmailOtpType.MagicLink);

		/// <summary>
		/// Log in a user given the token hash used in an email confirmation link.
		/// </summary>
		/// <param name="tokenHash"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		Task<TSession?> VerifyTokenHash(string tokenHash, EmailOtpType type = EmailOtpType.Email);

		/// <summary>
		/// Links an oauth identity to an existing user.
		///
		/// This method requires the PKCE flow.
		/// </summary>
		/// <param name="provider">Provider to Link</param>
		/// <param name="options"></param>
		/// <returns></returns>
		Task<ProviderAuthState> LinkIdentity(Provider provider, SignInOptions options);

		/// <summary>
		/// Unlinks an identity from a user by deleting it. The user will no longer be able to sign in with that identity once it's unlinked.
		/// </summary>
		/// <param name="userIdentity">Identity to be unlinked</param>
		/// <returns></returns>
		Task<bool> UnlinkIdentity(UserIdentity userIdentity);

		/// <summary>
		/// Add a listener to get errors that occur outside of a typical Exception flow.
		/// In particular, this is used to get errors and messages from the background thread
		/// that automatically manages refreshing the user's token.
		/// </summary>
		/// <param name="listener">Callback method for debug messages</param>
		void AddDebugListener(Action<string, Exception?> listener);

		/// <summary>
		/// Loads the session from the persistence layer.
		/// </summary>
		void LoadSession();

		/// <summary>
		/// Retrieves the settings from the server
		/// </summary>
		/// <returns></returns>
		Task<Settings?> Settings();

		/// <summary>
		/// Returns the client options.
		/// </summary>
		ClientOptions Options { get; }

		/// <summary>
		/// Get User details by JWT. Can be used to validate a JWT.
		/// </summary>
		/// <param name="jwt">A valid JWT. Must be a JWT that originates from a user.</param>
		/// <returns></returns>
		Task<TUser?> GetUser(string jwt);

		/// <summary>
		/// Posts messages and exceptions to the debug listener. This is particularly useful for sorting
		/// out issues with the refresh token background thread.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="e"></param>
		void Debug(string message, Exception? e = null);

		/// <summary>
		/// Let all of the listeners know that the stateless client is being shutdown.
		///
		/// In particular, the background thread that is used to refresh the token is stopped.
		/// </summary>
		public void Shutdown();

		/// <summary>
		/// Refreshes a Token using the current session.
		/// </summary>
		/// <returns></returns>
		public Task RefreshToken();

		#region MFA
		/// <summary>
		/// Starts the enrollment process for a new Multi-Factor Authentication (MFA)
		/// factor. This method creates a new `unverified` factor.
		/// To verify a factor, present the QR code or secret to the user and ask them to add it to their
		/// authenticator app.
		/// The user has to enter the code from their authenticator app to verify it.
		///
		/// Upon verifying a factor, all other sessions are logged out and the current session's authenticator level is promoted to `aal2`.
		/// </summary>
		Task<MfaEnrollResponse?> Enroll(MfaEnrollParams mfaEnrollParams);

		/// <summary>
		/// Prepares a challenge used to verify that a user has access to a MFA
		/// factor.
		/// </summary>
		Task<MfaChallengeResponse?> Challenge(MfaChallengeParams mfaChallengeParams);

		/// <summary>
		/// Verifies a code against a challenge. The verification code is
		/// provided by the user by entering a code seen in their authenticator app.
		/// </summary>
		Task<Session?> Verify(MfaVerifyParams mfaVerifyParams);

		/// <summary>
		/// Helper method which creates a challenge and immediately uses the given code to verify against it thereafter. The verification code is
		/// provided by the user by entering a code seen in their authenticator app.
		/// </summary>
		Task<Session?> ChallengeAndVerify(MfaChallengeAndVerifyParams mfaChallengeAndVerifyParams);

		/// <summary>
		/// Unenroll removes a MFA factor.
		/// A user has to have an `aal2` authenticator level in order to unenroll a `verified` factor.
		/// </summary>
		Task<MfaUnenrollResponse?> Unenroll(MfaUnenrollParams mfaUnenrollParams);

		/// <summary>
		/// Returns the list of MFA factors enabled for this user
		/// </summary>
		Task<MfaListFactorsResponse?> ListFactors();

		/// <summary>
		/// Returns the Authenticator Assurance Level (AAL) for the active session.
		///
		/// - `aal1` (or `null`) means that the user's identity has been verified only
		/// with a conventional login (email+password, OTP, magic link, social login,
		/// etc.).
		/// - `aal2` means that the user's identity has been verified both with a conventional login and at least one MFA factor.
		///
		/// Although this method returns a promise, it's fairly quick (microseconds)
		/// and rarely uses the network. You can use this to check whether the current
		/// user needs to be shown a screen to verify their MFA factors.
		/// </summary>
		Task<MfaGetAuthenticatorAssuranceLevelResponse?> GetAuthenticatorAssuranceLevel();

		#endregion

	}
}

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue.Mfa;
using static Supabase.Gotrue.Constants;
using static Supabase.Gotrue.Constants.AuthState;
using static Supabase.Gotrue.Exceptions.FailureHint.Reason;

namespace Supabase.Gotrue
{
	/// <inheritdoc />
	public class Client : IGotrueClient<User, Session>
	{
		/// <summary>
		/// The underlying API requests object that sends the requests
		/// </summary>
		private readonly IGotrueApi<User, Session> _api;

		/// <summary>
		/// Handlers for notifications of state changes.
		/// </summary>
		private readonly List<IGotrueClient<User, Session>.AuthEventHandler> _authEventHandlers =
			new List<IGotrueClient<User, Session>.AuthEventHandler>();

		/// <summary>
		/// Gets notifications if there is a failure not visible by exceptions (e.g. background thread refresh failure)
		/// </summary>
		private DebugNotification? _debugNotification;

		/// <summary>
		/// Object called to persist the session (e.g. filesystem or cookie)
		/// </summary>
		private IGotruePersistenceListener<Session>? _sessionPersistence;

		/// <summary>
		/// Get the TokenRefresh object, if it exists
		/// </summary>
		public TokenRefresh? TokenRefresh { get; }

		/// <summary>
		/// Initializes the GoTrue stateful client.
		///
		/// You will likely want to at least specify a <see>
		///     <cref>ClientOptions.Url</cref>
		/// </see>
		///
		/// Sessions are not automatically retrieved when this object is created.
		///
		/// If you want to load the session from your persistence store, <see>
		///     <cref>GotrueSessionPersistence</cref>
		/// </see>.
		///
		/// If you want to load/refresh the session, <see>
		///     <cref>RetrieveSessionAsync</cref>
		/// </see>.
		///
		/// For a typical client application, you'll want to load the session from persistence
		/// and then refresh it. If your application is listening for session changes, you'll
		/// get two SignIn notifications if the persisted session is valid - one for the
		/// session loaded from disk, and a second on a successful session refresh.
		///
		/// <remarks></remarks>
		/// <example>
		///		var client = new Supabase.Gotrue.Client(options);
		///     client.LoadSession();
		///		await client.RetrieveSessionAsync();
		/// </example>
		/// </summary>
		/// <param name="options"></param>
		public Client(ClientOptions? options = null)
		{
			options ??= new ClientOptions();
			Options = options;
			_api = new Api(options.Url, options.Headers);

			if (options.AutoRefreshToken)
			{
				TokenRefresh = new TokenRefresh(this);
				_authEventHandlers.Add(TokenRefresh.ManageAutoRefresh);
			}
		}

		/// <inheritdoc />
		public void SetPersistence(IGotrueSessionPersistence<Session> persistence)
		{
			if (_sessionPersistence != null) _authEventHandlers.Remove(_sessionPersistence.EventHandler);
			_sessionPersistence = new PersistenceListener(persistence);
			_authEventHandlers.Add(_sessionPersistence.EventHandler);
		}

		/// <inheritdoc />
		public ClientOptions Options { get; }

		/// <inheritdoc />
		public Task<User?> GetUser(string jwt) => _api.GetUser(jwt);

		/// <inheritdoc />
		public void NotifyAuthStateChange(AuthState stateChanged)
		{
			foreach (var handler in _authEventHandlers)
			{
				try
				{
					handler.Invoke(this, stateChanged);
				}
				catch (Exception e)
				{
					_debugNotification?.Log("Auth State Change Handler Failure", e);
				}
			}
		}

		/// <inheritdoc />
		public User? CurrentUser
		{
			get => CurrentSession?.User;
		}

		/// <inheritdoc />
		public void AddStateChangedListener(IGotrueClient<User, Session>.AuthEventHandler authEventHandler)
		{
			if (_authEventHandlers.Contains(authEventHandler)) return;

			_authEventHandlers.Add(authEventHandler);
		}

		/// <inheritdoc />
		public void RemoveStateChangedListener(IGotrueClient<User, Session>.AuthEventHandler authEventHandler)
		{
			if (!_authEventHandlers.Contains(authEventHandler)) return;

			_authEventHandlers.Remove(authEventHandler);
		}


		/// <inheritdoc />
		public void ClearStateChangedListeners()
		{
			_authEventHandlers.Clear();
		}

		/// <inheritdoc />
		public bool Online { get; set; } = true;

		/// <inheritdoc />
		public Session? CurrentSession { get; private set; }


		/// <inheritdoc />
		public Task<Session?> SignUp(string email, string password, SignUpOptions? options = null) =>
			SignUp(SignUpType.Email, email, password, options);


		/// <inheritdoc />
		public async Task<Session?> SignUp(SignUpType type, string identifier, string password,
			SignUpOptions? options = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var session = type switch
			{
				SignUpType.Email => await _api.SignUpWithEmail(identifier, password, options),
				SignUpType.Phone => await _api.SignUpWithPhone(identifier, password, options),
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};

			if (session?.User?.ConfirmedAt != null || session?.User != null && Options.AllowUnconfirmedUserSessions)
			{
				UpdateSession(session);
				NotifyAuthStateChange(SignedIn);
				return CurrentSession;
			}

			return session;
		}

		/// <inheritdoc />
		public async Task<bool> SignIn(string email, SignInOptions? options = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			await _api.SendMagicLinkEmail(email, options);
			return true;
		}

		/// <inheritdoc />
		public async Task<Session?> SignInWithIdToken(Provider provider, string idToken, string? accessToken = null, string? nonce = null,
			string? captchaToken = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var result = await _api.SignInWithIdToken(provider, idToken, accessToken, nonce, captchaToken);

			UpdateSession(result);
			NotifyAuthStateChange(SignedIn);

			return result;
		}


		/// <inheritdoc />
		public async Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessEmailOptions options)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();
			return await _api.SignInWithOtp(options);
		}


		/// <inheritdoc />
		public async Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessPhoneOptions options)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();
			return await _api.SignInWithOtp(options);
		}


		/// <inheritdoc />
		public Task<bool> SendMagicLink(string email, SignInOptions? options = null) => SignIn(email, options);

		/// <inheritdoc />
		public Task<Session?> SignIn(string email, string password) => SignIn(SignInType.Email, email, password);

		/// <inheritdoc />
		public Task<Session?> SignInWithPassword(string email, string password) => SignIn(email, password);


		/// <inheritdoc />
		public async Task<Session?> SignIn(SignInType type, string identifierOrToken, string? password = null,
			string? scopes = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			Session? newSession;
			switch (type)
			{
				case SignInType.Email:
					newSession = await _api.SignInWithEmail(identifierOrToken, password!);
					UpdateSession(newSession);
					break;
				case SignInType.Phone:
					if (string.IsNullOrEmpty(password))
					{
						await _api.SendMobileOTP(identifierOrToken);
						return null;
					}
					newSession = await _api.SignInWithPhone(identifierOrToken, password!);
					UpdateSession(newSession);
					break;
				case SignInType.RefreshToken:
					if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
						throw new GotrueException("Not logged in.", NoSessionFound);

					await RefreshToken(CurrentSession.AccessToken!, identifierOrToken);
					return CurrentSession;
				default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}

			// Handle case when a user registers and has not confirmed email (and options do not allow for this), return null for session.
			if (newSession?.User?.ConfirmedAt == null &&
			    (newSession?.User == null || !Options.AllowUnconfirmedUserSessions))
				return null;

			NotifyAuthStateChange(SignedIn);
			return CurrentSession;
		}


		/// <inheritdoc />
		public Task<ProviderAuthState> SignIn(Provider provider, SignInOptions? options = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var providerUri = _api.GetUriForProvider(provider, options);
			return Task.FromResult(providerUri);
		}

		/// <inheritdoc />
		public Task<SSOResponse?> SignInWithSSO(Guid providerId, SignInWithSSOOptions? options = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			return _api.SignInWithSSO(providerId, options);
		}

		/// <inheritdoc />
		public Task<SSOResponse?> SignInWithSSO(string domain, SignInWithSSOOptions? options = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			return _api.SignInWithSSO(domain, options);
		}

		/// <inheritdoc />
		public async Task<Session?> SignInAnonymously(SignInAnonymouslyOptions? options = null)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var newSession = await _api.SignInAnonymously(options);
			UpdateSession(newSession);

			NotifyAuthStateChange(SignedIn);
			return CurrentSession;
		}

		/// <inheritdoc />
		public async Task<Session?> VerifyOTP(string phone, string token, MobileOtpType type = MobileOtpType.SMS)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var session = await _api.VerifyMobileOTP(phone, token, type);

			if (session?.AccessToken != null)
			{
				UpdateSession(session);
				NotifyAuthStateChange(SignedIn);
				return session;
			}

			return null;
		}


		/// <inheritdoc />
		public async Task<Session?> VerifyOTP(string email, string token, EmailOtpType type = EmailOtpType.MagicLink)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var session = await _api.VerifyEmailOTP(email, token, type);

			if (session?.AccessToken != null)
			{
				UpdateSession(session);
				NotifyAuthStateChange(SignedIn);
				return session;
			}

			return null;
		}

		/// <inheritdoc />
		public async Task<Session?> VerifyTokenHash(string tokenHash, EmailOtpType type = EmailOtpType.Email)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			DestroySession();

			var session = await _api.VerifyTokenHash(tokenHash, type);

			if (session?.AccessToken != null)
			{
				UpdateSession(session);
				NotifyAuthStateChange(SignedIn);
				return session;
			}

			return null;
		}

		/// <inheritdoc />
		public Task<ProviderAuthState> LinkIdentity(Provider provider, SignInOptions options)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			if (CurrentSession == null || CurrentUser == null)
				throw new GotrueException("A valid session is required.", NoSessionFound);

			if (options.FlowType != OAuthFlowType.PKCE)
				throw new GotrueException("PKCE flow type is required for this action.", InvalidFlowType);

			return _api.LinkIdentity(CurrentSession.AccessToken!, provider, options);
		}

		/// <inheritdoc />
		public Task<bool> UnlinkIdentity(UserIdentity userIdentity)
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			if (CurrentSession == null || CurrentUser == null)
				throw new GotrueException("A valid session is required.", NoSessionFound);

			return _api.UnlinkIdentity(CurrentSession.AccessToken!, userIdentity);
		}

		/// <inheritdoc />
		public async Task SignOut(SignOutScope scope = SignOutScope.Global)
		{
			if (CurrentSession?.AccessToken != null) await _api.SignOut(CurrentSession.AccessToken, scope);
			UpdateSession(null);
			NotifyAuthStateChange(SignedOut);
		}


		/// <inheritdoc />
		public async Task<User?> Update(UserAttributes attributes)
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.");

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			var result = await _api.UpdateUser(CurrentSession.AccessToken!, attributes);
			CurrentSession.User = result;
			NotifyAuthStateChange(UserUpdated);

			return result;
		}


		/// <inheritdoc />
		public async Task<bool> Reauthenticate()
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			var response = await _api.Reauthenticate(CurrentSession.AccessToken!);

			return response.ResponseMessage?.IsSuccessStatusCode ?? false;
		}

		/// <inheritdoc />
		public async Task<bool> ResetPasswordForEmail(string email)
		{
			var result = await _api.ResetPasswordForEmail(email);
			result.ResponseMessage?.EnsureSuccessStatusCode();
			return true;
		}

		/// <inheritdoc />
		public async Task<ResetPasswordForEmailState> ResetPasswordForEmail(ResetPasswordForEmailOptions options)
		{
			var state = await _api.ResetPasswordForEmail(options);
			return state;
		}

		/// <inheritdoc />
		public async Task<Session?> RefreshSession()
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			await RefreshToken();

			var user = await _api.GetUser(CurrentSession.AccessToken!);
			CurrentSession.User = user;

			return CurrentSession;
		}

		/// <inheritdoc />
		public async Task<Session> SetSession(string accessToken, string refreshToken, bool forceAccessTokenRefresh = false)
		{
			DestroySession();

			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
				throw new GotrueException("`accessToken` and `refreshToken` cannot be empty.", NoSessionFound);

			var payload = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Payload;

			if (payload == null || payload.ValidTo == DateTime.MinValue)
				throw new GotrueException("`accessToken`'s payload was of an unknown structure.", NoSessionFound);

			if (payload.ValidTo < DateTime.UtcNow || forceAccessTokenRefresh)
			{
				var result = await _api.RefreshAccessToken(accessToken, refreshToken);

				if (result == null || string.IsNullOrEmpty(result.AccessToken))
					throw new GotrueException("Could not generate a session given the provided parameters.", NoSessionFound);

				CurrentSession = result;
				NotifyAuthStateChange(SignedIn);
				return CurrentSession;
			}

			CurrentSession = new Session
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken,
				TokenType = "bearer",
				ExpiresIn = payload.Expiration!.Value,
				User = await _api.GetUser(accessToken)
			};

			NotifyAuthStateChange(SignedIn);
			return CurrentSession;
		}

		/// <summary>
		/// Parses a <see cref="Session"/> out of a <see cref="Uri"/>'s Query parameters.
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

			if (!string.IsNullOrEmpty(errorDescription)) throw new GotrueException(errorDescription, BadSessionUrl);

			var accessToken = query.Get("access_token");

			if (string.IsNullOrEmpty(accessToken))
				throw new GotrueException("No access_token detected.", BadSessionUrl);

			var expiresIn = query.Get("expires_in");

			if (string.IsNullOrEmpty(expiresIn)) throw new GotrueException("No expires_in detected.", BadSessionUrl);

			var refreshToken = query.Get("refresh_token");

			if (string.IsNullOrEmpty(refreshToken))
				throw new GotrueException("No refresh_token detected.", BadSessionUrl);

			var tokenType = query.Get("token_type");

			if (string.IsNullOrEmpty(tokenType)) throw new GotrueException("No token_type detected.", BadSessionUrl);

			var user = await _api.GetUser(accessToken);

			var session = new Session
			{
				AccessToken = accessToken,
				ExpiresIn = long.Parse(expiresIn),
				RefreshToken = refreshToken,
				TokenType = tokenType,
				User = user
			};

			if (storeSession)
			{
				UpdateSession(session);
				NotifyAuthStateChange(SignedIn);

				if (query.Get("type") == "recovery") NotifyAuthStateChange(PasswordRecovery);
			}

			return session;
		}

		/// <inheritdoc />
		public async Task<Session?> RetrieveSessionAsync()
		{
			// No session, so just return.
			if (CurrentSession == null)
				return null;

			// Check to see if the session has expired. If so go ahead and destroy it.
			if (CurrentSession != null && CurrentSession.Expired())
			{
				_debugNotification?.Log($"Loaded session has expired");
				DestroySession();
				return null;
			}

			// If we aren't online, we can't refresh the token
			if (!Online)
			{
				throw new GotrueException("Only supported when online", Offline);
			}

			// We have a session, and hasn't expired, and we seem to be online. Let's try to refresh it.
			if (Options.AutoRefreshToken && CurrentSession?.RefreshToken != null)
			{
				try
				{
					await RefreshToken();
					return CurrentSession;
				}
				catch (Exception e)
				{
					_debugNotification?.Log($"Failed to refresh token ({e.Message})", e);
					_debugNotification?.Log(JsonConvert.SerializeObject(CurrentSession, Formatting.Indented));
					DestroySession();
					return null;
				}
			}

			return CurrentSession;
		}


		/// <inheritdoc />
		public async Task<Session?> ExchangeCodeForSession(string codeVerifier, string authCode)
		{
			var result = await _api.ExchangeCodeForSession(codeVerifier, authCode);

			if (result != null)
			{
				UpdateSession(result);
				NotifyAuthStateChange(SignedIn);
				return CurrentSession;
			}

			return null;
		}

		/// <summary>
		/// Headers sent to the API on every request.
		/// </summary>
		public Func<Dictionary<string, string>>? GetHeaders
		{
			get => _api.GetHeaders;
			set => _api.GetHeaders = value;
		}


		/// <inheritdoc />
		public void AddDebugListener(Action<string, Exception?> listener)
		{
			_debugNotification ??= new DebugNotification();
			_debugNotification.AddDebugListener(listener);
		}

		/// <summary>
		/// Saves the session
		/// </summary>
		/// <param name="session"></param>
		private void UpdateSession(Session? session)
		{
			if (session == null)
			{
				CurrentSession = null;
				NotifyAuthStateChange(SignedOut);
				return;
			}

			var dirty = CurrentSession != session;
			CurrentSession = session;
			if (dirty) NotifyAuthStateChange(UserUpdated);
		}

		/// <summary>
		/// Clears the session
		/// </summary>
		private void DestroySession()
		{
			UpdateSession(null);
		}

		/// <inheritdoc />
		public async Task RefreshToken(string accessToken, string refreshToken)
		{
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
				throw new GotrueException("No token provided", NoSessionFound);

			var result = await _api.RefreshAccessToken(accessToken, refreshToken);

			if (result == null || string.IsNullOrEmpty(result.AccessToken))
				throw new GotrueException("Could not refresh token from provided session.", NoSessionFound);

			CurrentSession = result;
			NotifyAuthStateChange(TokenRefreshed);
		}

		/// <inheritdoc />
		public async Task RefreshToken()
		{
			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession?.AccessToken) || string.IsNullOrEmpty(CurrentSession?.RefreshToken))
				throw new GotrueException("No current session.", NoSessionFound);

			if (CurrentSession!.Expired())
				throw new GotrueException("Session expired", ExpiredRefreshToken);

			var result = await _api.RefreshAccessToken(CurrentSession.AccessToken!, CurrentSession.RefreshToken!);

			if (result == null || string.IsNullOrEmpty(result.AccessToken))
				throw new GotrueException("Could not refresh token from provided session.", NoSessionFound);

			CurrentSession = result;

			NotifyAuthStateChange(TokenRefreshed);
		}


		/// <inheritdoc />
		public void LoadSession()
		{
			if (_sessionPersistence != null) UpdateSession(_sessionPersistence.Persistence.LoadSession());
		}


		/// <inheritdoc />
		public Task<Settings?> Settings()
		{
			if (!Online)
				return Task.FromResult<Settings?>(null);

			return _api.Settings();
		}

		/// <inheritdoc />
		public void Debug(string message, Exception? e = null)
		{
			_debugNotification?.Log(message, e);
		}

		/// <inheritdoc />
		public void Shutdown()
		{
			NotifyAuthStateChange(AuthState.Shutdown);
		}

		/// <inheritdoc />
		public async Task<MfaEnrollResponse?> Enroll(MfaEnrollParams mfaEnrollParams)
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			return await _api.Enroll(CurrentSession.AccessToken, mfaEnrollParams);
		}

		/// <inheritdoc />
		public async Task<MfaChallengeResponse?> Challenge(MfaChallengeParams mfaChallengeParams)
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			return await _api.Challenge(CurrentSession.AccessToken, mfaChallengeParams);
		}

		/// <inheritdoc />
		public async Task<Session?> Verify(MfaVerifyParams mfaVerifyParams)
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			var result =  await _api.Verify(CurrentSession.AccessToken, mfaVerifyParams);

			if (result == null || string.IsNullOrEmpty(result.AccessToken))
				throw new GotrueException("Could not verify MFA.", MfaChallengeUnverified);

			var session = new Session
			{
				AccessToken = result.AccessToken,
				RefreshToken = result.RefreshToken,
				TokenType = "bearer",
				ExpiresIn = result.ExpiresIn,
				User = result.User
			};

			UpdateSession(session);
			NotifyAuthStateChange(MfaChallengeVerified);

			return session;
		}

		/// <inheritdoc />
		public async Task<Session?> ChallengeAndVerify(MfaChallengeAndVerifyParams mfaChallengeAndVerifyParams)
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			var challengeResponse = await _api.Challenge(CurrentSession.AccessToken, new MfaChallengeParams
			{
				FactorId = mfaChallengeAndVerifyParams.FactorId
			});

			if (challengeResponse == null)
			{
				return null;
			}

			var result =  await _api.Verify(CurrentSession.AccessToken, new MfaVerifyParams
			{
				FactorId = mfaChallengeAndVerifyParams.FactorId,
				Code = mfaChallengeAndVerifyParams.Code,
				ChallengeId = challengeResponse.Id
			});

			if (result == null || string.IsNullOrEmpty(result.AccessToken))
				throw new GotrueException("Could not verify MFA.", MfaChallengeUnverified);

			var session = new Session
			{
				AccessToken = result.AccessToken,
				RefreshToken = result.RefreshToken,
				TokenType = "bearer",
				ExpiresIn = result.ExpiresIn,
				User = result.User
			};

			UpdateSession(session);
			NotifyAuthStateChange(MfaChallengeVerified);

			return session;
		}

		/// <inheritdoc />
		public async Task<MfaUnenrollResponse?> Unenroll(MfaUnenrollParams mfaUnenrollParams)
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			if (!Online)
				throw new GotrueException("Only supported when online", Offline);

			return  await _api.Unenroll(CurrentSession.AccessToken, mfaUnenrollParams);
		}

		/// <inheritdoc />
		public Task<MfaListFactorsResponse?> ListFactors()
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			var response = new MfaListFactorsResponse()
			{
				All = CurrentSession.User!.Factors,
				Totp = CurrentSession.User!.Factors?.Where(x => x.FactorType == "totp" && x.Status == "verified").ToList()
			};

			return Task.FromResult(response);
		}

		public Task<MfaGetAuthenticatorAssuranceLevelResponse?> GetAuthenticatorAssuranceLevel()
		{
			if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.AccessToken))
				throw new GotrueException("Not Logged in.", NoSessionFound);

			var payload = new JwtSecurityTokenHandler().ReadJwtToken(CurrentSession.AccessToken).Payload;

			if (payload == null || payload.ValidTo == DateTime.MinValue)
				throw new GotrueException("`accessToken`'s payload was of an unknown structure.", NoSessionFound);

			AuthenticatorAssuranceLevel? currentLevel = null;

			if (payload.ContainsKey("aal"))
			{
				currentLevel = Enum.TryParse(payload["aal"].ToString(), out AuthenticatorAssuranceLevel parsedLevel) ? parsedLevel : (AuthenticatorAssuranceLevel?)null;
			}

			AuthenticatorAssuranceLevel? nextLevel = currentLevel;

			var verifiedFactors = CurrentSession.User!.Factors?.Where(factor => factor.Status == "verified").ToList() ?? new List<Factor>();
			if (verifiedFactors.Count > 0)
			{
				nextLevel = AuthenticatorAssuranceLevel.aal2;
			}

			var currentAuthenticationMethods = payload.Amr.Select(x => JsonConvert.DeserializeObject<AmrEntry>(x));

			var response = new MfaGetAuthenticatorAssuranceLevelResponse
			{
				CurrentLevel = currentLevel,
				NextLevel = nextLevel,
				CurrentAuthenticationMethods = currentAuthenticationMethods.ToArray()
			};

			return Task.FromResult(response);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Core;
using Supabase.Core.Attributes;
using Supabase.Core.Extensions;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue.Mfa;
using Supabase.Gotrue.Responses;
using static Supabase.Gotrue.Constants;

namespace Supabase.Gotrue
{
	/// <summary>
	/// The REST calls to the Gotrue API.
	/// </summary>
	public class Api : IGotrueApi<User, Session>
	{
		private string Url { get; }

		/// <summary>
		/// Function that can be set to return dynamic headers.
		/// Headers specified in the constructor will ALWAYS take precedence over headers returned by this function.
		/// </summary>
		public Func<Dictionary<string, string>>? GetHeaders { get; set; }

		private Dictionary<string, string> _headers;
		/// <summary>
		/// Headers to be sent with every request. These will be merged with any headers returned by GetHeaders.
		/// </summary>
		protected Dictionary<string, string> Headers
		{
			get => GetHeaders != null ? GetHeaders().MergeLeft(_headers) : _headers;
			set
			{
				_headers = value;

				if (!_headers.ContainsKey("X-Client-Info"))
					_headers.Add("X-Client-Info", Util.GetAssemblyVersion(typeof(Client)));
			}
		}

		/// <summary>
		/// Creates a new API client
		/// </summary>
		/// <param name="url"></param>
		/// <param name="headers"></param>
		public Api(string url, Dictionary<string, string>? headers = null)
		{
			Url = url;
			headers ??= new Dictionary<string, string>();
			_headers = headers;
		}

		/// <summary>
		/// Signs a user up using an email address and password.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <param name="options">Optional Signup data.</param>
		/// <returns></returns>
		public async Task<Session?> SignUpWithEmail(string email, string password, SignUpOptions? options = null)
		{
			var body = new Dictionary<string, object> { { "email", email }, { "password", password } };
			var endpoint = $"{Url}/signup";

			if (options != null)
			{
				if (!string.IsNullOrEmpty(options.RedirectTo))
				{
					endpoint = Helpers.AddQueryParams(endpoint, new Dictionary<string, string> { { "redirect_to", options.RedirectTo! } }).ToString();
				}

				if (options.Data != null)
				{
					body.Add("data", options.Data);
				}
			}

			var response = await Helpers.MakeRequest(HttpMethod.Post, endpoint, body, Headers);

			if (!string.IsNullOrEmpty(response.Content))
			{
				// Gotrue returns a Session object for an auto-/pre-confirmed account
				var session = JsonConvert.DeserializeObject<Session>(response.Content!);

				// If account is unconfirmed, Gotrue returned the user object, so fill User data
				// in from the parsed response.
				if (session is { User: null })
				{
					// Gotrue returns a User object for an unconfirmed account
					session.User = JsonConvert.DeserializeObject<User>(response.Content!);
				}

				return session;
			}
			return null;
		}

		/// <summary>
		/// Logs in an existing user using their email address.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public Task<Session?> SignInWithEmail(string email, string password)
		{
			var body = new Dictionary<string, object> { { "email", email }, { "password", password } };
			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/token?grant_type=password", body, Headers);
		}

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
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessEmailOptions options)
		{
			var url = string.IsNullOrEmpty(options.EmailRedirectTo) ? $"{Url}/otp" : $"{Url}/otp?redirect_to={options.EmailRedirectTo}";
			string? verifier = null;

			var body = new Dictionary<string, object>
			{
				{ "email", options.Email },
				{ "data", options.Data },
				{ "create_user", options.ShouldCreateUser }
			};

			if (options.FlowType == OAuthFlowType.PKCE)
			{
				var challenge = Helpers.GenerateNonce();
				verifier = Helpers.GeneratePKCENonceVerifier(challenge);

				body.Add("code_challenge", challenge);
				body.Add("code_challenge_method", "s256");
			}

			if (!string.IsNullOrEmpty(options.CaptchaToken))
				body.Add("gotrue_meta_security", new Dictionary<string, string> { { "captcha_token", options.CaptchaToken! } });

			await Helpers.MakeRequest(HttpMethod.Post, url, body, Headers);

			return new PasswordlessSignInState { PKCEVerifier = verifier };
		}

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
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task<PasswordlessSignInState> SignInWithOtp(SignInWithPasswordlessPhoneOptions options)
		{
			var url = $"{Url}/otp";

			var body = new Dictionary<string, object>
			{
				{ "phone", options.Phone },
				{ "data", options.Data },
				{ "create_user", options.ShouldCreateUser },
				{ "channel", Core.Helpers.GetMappedToAttr(options.Channel).Mapping }
			};

			if (!string.IsNullOrEmpty(options.CaptchaToken))
				body.Add("gotrue_meta_security", new Dictionary<string, string> { { "captcha_token", options.CaptchaToken! } });

			await Helpers.MakeRequest(HttpMethod.Post, url, body, Headers);

			return new PasswordlessSignInState();
		}

		/// <summary>
		/// Creates a new anonymous user.
		/// </summary>
		/// <param name="options"></param>
		/// <returns>A session where the is_anonymous claim in the access token JWT set to true</returns>
		public async Task<Session?> SignInAnonymously(SignInAnonymouslyOptions? options = null)
		{
			var url = $"{Url}/signup";

			var body = new Dictionary<string, object>();

			if (options?.Data != null)
				body.Add("data", options.Data);

			if (options != null && !string.IsNullOrEmpty(options.CaptchaToken))
				body.Add("gotrue_meta_security", new Dictionary<string, string> { { "captcha_token", options.CaptchaToken! } });

			return await Helpers.MakeRequest<Session>(HttpMethod.Post, url, body, Headers);
		}

		/// <summary>
		/// Allows signing in with an ID token issued by certain supported providers.
		/// The [idToken] is verified for validity and a new session is established.
		/// This method of signing in only supports [Provider.Google] or [Provider.Apple].
		/// </summary>
		/// <param name="provider">A supported provider (Google, Apple, Azure, Facebook)</param>
		/// <param name="idToken">OIDC ID token issued by the specified provider. The `iss` claim in the ID token must match the supplied provider. Some ID tokens contain an `at_hash` which require that you provide an `access_token` value to be accepted properly. If the token contains a `nonce` claim you must supply the nonce used to obtain the ID token.</param>
		/// <param name="accessToken">If the ID token contains an `at_hash` claim, then the hash of this value is compared to the value in the ID token.</param>
		/// <param name="nonce">If the ID token contains a `nonce` claim, then the hash of this value is compared to the value in the ID token.</param>
		/// <param name="captchaToken">Verification token received when the user completes the captcha on the site.</param>
		/// <returns></returns>
		/// <exception>
		///     <cref>InvalidProviderException</cref>
		/// </exception>
		public Task<Session?> SignInWithIdToken(Provider provider, string idToken, string? accessToken = null, string? nonce = null, string? captchaToken = null)
		{
			if (provider != Provider.Google && provider != Provider.Apple && provider != Provider.Azure && provider != Provider.Facebook)
				throw new GotrueException($"Provider must be `Google`, `Apple`, `Azure`, or `Facebook` not {provider}");

			var body = new Dictionary<string, object?>
			{
				{ "provider", Core.Helpers.GetMappedToAttr(provider).Mapping },
				{ "id_token", idToken },
			};

			if (!string.IsNullOrEmpty(accessToken))
				body.Add("access_token", accessToken);

			if (!string.IsNullOrEmpty(nonce))
				body.Add("nonce", nonce);

			if (!string.IsNullOrEmpty(captchaToken))
				body.Add("gotrue_meta_security", new Dictionary<string, object?> { { "captcha_token", captchaToken } });

			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/token?grant_type=id_token", body, Headers);
		}

		private Task<SSOResponse?> SignInWithSsoInternal(Guid? providerId = null, string? domain = null, SignInWithSSOOptions? options = null)
		{
			if(providerId != null && domain != null)
				throw new GotrueException($"Both providerId and domain were provided to the API, " +
				                          $"you must supply either one or the other but not both providerId={providerId}, domain={domain}");
			if(providerId == null && domain == null)
				throw new GotrueException($"Both providerId and domain were null " +
				                          $"you must supply either one or the other but not both providerId={providerId}, domain={domain}");

			string? codeChallenge = null;
			string? codeChallengeMethod = null;
			if (options?.FlowType == OAuthFlowType.PKCE)
			{
				var codeVerifier = Helpers.GenerateNonce();
				codeChallenge = Helpers.GeneratePKCENonceVerifier(codeVerifier);
				codeChallengeMethod = "s256";
			}

			var body = new Dictionary<string, object?>
			{
				{  providerId != null ? "provider_id" : "domain",  providerId != null ? providerId.ToString() : domain},
				{ "redirect_to", options?.RedirectTo },

				// this is important, it will not auto redirect the request and instead return the Uri needed to handle the login
				// without this in the body the request will automatically redirect to the providers sign in page
				{ "skip_http_redirect", true },

				{ "code_challenge", codeChallenge },
				{ "code_challenge_method", codeChallengeMethod }
			};

			if (!string.IsNullOrEmpty(options?.CaptchaToken))
				body.Add("gotrue_meta_security", new Dictionary<string, object?> { { "captcha_token", options?.CaptchaToken } });

			return Helpers.MakeRequest<SSOResponse>(HttpMethod.Post, $"{Url}/sso", body, Headers);
		}

		/// <inheritdoc />
		public Task<SSOResponse?> SignInWithSSO(Guid providerId, SignInWithSSOOptions? options = null)
		{
			return SignInWithSsoInternal(providerId: providerId, options: options);
		}

		/// <inheritdoc />
		public Task<SSOResponse?> SignInWithSSO(string domain, SignInWithSSOOptions? options = null)
		{
			return SignInWithSsoInternal(domain: domain, options: options);
		}

		/// <summary>
		/// Sends a magic login link to an email address.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public Task<BaseResponse> SendMagicLinkEmail(string email, SignInOptions? options = null)
		{
			var data = new Dictionary<string, string> { { "email", email } };

			var endpoint = $"{Url}/magiclink";

			if (options != null)
			{
				if (!string.IsNullOrEmpty(options.RedirectTo))
				{
					endpoint = Helpers.AddQueryParams(endpoint, new Dictionary<string, string> { { "redirect_to", options.RedirectTo! } }).ToString();
				}
			}

			return Helpers.MakeRequest(HttpMethod.Post, endpoint, data, Headers);
		}

		/// <summary>
		/// Sends an invite link to an email address.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="jwt">this token needs role 'supabase_admin' or 'service_role'</param>
		/// <param name="options"></param>
		/// <returns></returns>
		public Task<BaseResponse> InviteUserByEmail(string email, string jwt, InviteUserByEmailOptions? options = null)
		{
			var url = options == null || string.IsNullOrEmpty(options.RedirectTo) ? $"{Url}/invite" : $"{Url}/invite?redirect_to={options.RedirectTo}";
			var body = new Dictionary<string, object> { { "email", email } };

			if (options?.Data != null)
				body["data"] = options.Data;

			return Helpers.MakeRequest(HttpMethod.Post, url, body, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Signs up a new user using their phone number and a password.The phone number of the user.
		/// </summary>
		/// <param name="phone">The phone number of the user.</param>
		/// <param name="password">The password of the user.</param>
		/// <param name="options">Optional Signup data.</param>
		/// <returns></returns>
		public Task<Session?> SignUpWithPhone(string phone, string password, SignUpOptions? options = null)
		{
			if (string.IsNullOrEmpty(phone))
				throw new GotrueException("Phone number not provided.", FailureHint.Reason.UserBadPhoneNumber);

			var body = new Dictionary<string, object>
			{
				{ "phone", phone },
				{ "password", password },
			};

			string endpoint = $"{Url}/signup";

			if (options != null)
			{
				if (!string.IsNullOrEmpty(options.RedirectTo))
				{
					endpoint = Helpers.AddQueryParams(endpoint, new Dictionary<string, string> { { "redirect_to", options.RedirectTo! } }).ToString();
				}

				if (options.Data != null)
				{
					body.Add("data", options.Data);
				}
			}

			return Helpers.MakeRequest<Session>(HttpMethod.Post, endpoint, body, Headers);
		}

		/// <summary>
		/// Logs in an existing user using their phone number and password.
		/// </summary>
		/// <param name="phone">The phone number of the user.</param>
		/// <param name="password">The password of the user.</param>
		/// <returns></returns>
		public Task<Session?> SignInWithPhone(string phone, string password)
		{
			var data = new Dictionary<string, object>
			{
				{ "phone", phone },
				{ "password", password }
			};
			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/token?grant_type=password", data, Headers);
		}

		/// <summary>
		/// Sends a mobile OTP via SMS. Will register the account if it doesn't already exist
		/// </summary>
		/// <param name="phone">phone The user's phone number WITH international prefix</param>
		/// <returns></returns>
		public Task<BaseResponse> SendMobileOTP(string phone)
		{
			var data = new Dictionary<string, string> { { "phone", phone } };
			return Helpers.MakeRequest(HttpMethod.Post, $"{Url}/otp", data, Headers);
		}

		/// <summary>
		/// Send User supplied Mobile OTP to be verified
		/// </summary>
		/// <param name="phone">The user's phone number WITH international prefix</param>
		/// <param name="token">token that user was sent to their mobile phone</param>
		/// <param name="type">e.g. SMS or phone change</param>
		/// <returns></returns>
		public Task<Session?> VerifyMobileOTP(string phone, string token, MobileOtpType type)
		{
			var data = new Dictionary<string, string>
			{
				{ "phone", phone },
				{ "token", token },
				{ "type", Core.Helpers.GetMappedToAttr(type).Mapping }
			};
			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/verify", data, Headers);
		}

		/// <summary>
		/// Send User supplied Email OTP to be verified
		/// </summary>
		/// <param name="email">The user's email address</param>
		/// <param name="token">token that user was sent to their mobile phone</param>
		/// <param name="type">Type of verification, e.g. invite, recovery, etc.</param>
		/// <returns></returns>
		public Task<Session?> VerifyEmailOTP(string email, string token, EmailOtpType type)
		{
			var data = new Dictionary<string, string>
			{
				{ "email", email },
				{ "token", token },
				{ "type", Core.Helpers.GetMappedToAttr(type).Mapping }
			};
			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/verify", data, Headers);
		}

		/// <summary>
		/// Verify token hash used in an email confirmation link.
		/// </summary>
		/// <param name="tokenHash">The token hash used in an email confirmation link</param>
		/// <param name="type">Type of verification, e.g. email.</param>
		/// <returns></returns>
		public Task<Session?> VerifyTokenHash(string tokenHash, EmailOtpType type)
		{
			var data = new Dictionary<string, string>
			{
				{ "token_hash", tokenHash },
				{ "type", Core.Helpers.GetMappedToAttr(type).Mapping }
			};
			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/verify", data, Headers);
		}

		/// <summary>
		/// Sends a reset request to an email address.
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		public Task<BaseResponse> ResetPasswordForEmail(string email)
		{
			var data = new Dictionary<string, string> { { "email", email } };
			return Helpers.MakeRequest(HttpMethod.Post, $"{Url}/recover", data, Headers);
		}

		/// <summary>
		/// Sends a password reset request to an email address.
		///
		/// This Method supports the PKCE Flow
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task<ResetPasswordForEmailState> ResetPasswordForEmail(ResetPasswordForEmailOptions options)
		{
			var url = string.IsNullOrEmpty(options.RedirectTo) ? $"{Url}/recover" : $"{Url}/recover?redirect_to={options.RedirectTo}";
			string? verifier = null;

			var body = new Dictionary<string, object>
			{
				{ "email", options.Email },
			};

			if (options.FlowType == OAuthFlowType.PKCE)
			{
				var challenge = Helpers.GenerateNonce();
				verifier = Helpers.GeneratePKCENonceVerifier(challenge);

				body.Add("code_challenge", challenge);
				body.Add("code_challenge_method", "s256");
			}

			if (!string.IsNullOrEmpty(options.CaptchaToken))
				body.Add("gotrue_meta_security", new Dictionary<string, string> { { "captcha_token", options.CaptchaToken! } });

			await Helpers.MakeRequest(HttpMethod.Post, url, body, Headers);

			return new ResetPasswordForEmailState { PKCEVerifier = verifier };
		}

		/// <summary>
		/// Create a temporary object with all configured headers and adds the Authorization token to be used on request methods
		/// </summary>
		/// <param name="jwt">JWT</param>
		/// <returns></returns>
		private Dictionary<string, string> CreateAuthedRequestHeaders(string jwt)
		{
			var headers = new Dictionary<string, string>(Headers)
			{
				["Authorization"] = $"Bearer {jwt}"
			};

			return headers;
		}

		/// <inheritdoc />
		public ProviderAuthState GetUriForProvider(Provider provider, SignInOptions? options = null) =>
			Helpers.GetUrlForProvider($"{Url}/authorize", provider, options);

		/// <summary>
		/// Log in an existing user via code from third-party provider.
		/// </summary>
		/// <param name="codeVerifier">Generated verifier (probably from GetUrlForProvider)</param>
		/// <param name="authCode">The received Auth Code Callback</param>
		/// <returns></returns>
		public Task<Session?> ExchangeCodeForSession(string codeVerifier, string authCode)
		{
			var url = new UriBuilder($"{Url}/token?grant_type=pkce");
			var body = new Dictionary<string, object>
			{
				{ "auth_code", authCode },
				{ "code_verifier", codeVerifier }
			};

			return Helpers.MakeRequest<Session>(HttpMethod.Post, url.ToString(), body, Headers);
		}

		/// <inheritdoc />
		public Task<MfaEnrollResponse?> Enroll(string jwt, MfaEnrollParams mfaEnrollParams)
		{
			var body = new Dictionary<string, object>
			{
				{ "friendly_name", mfaEnrollParams.FriendlyName },
				{ "factor_type", mfaEnrollParams.FactorType },
				{ "issuer", mfaEnrollParams.Issuer }
			};

			return Helpers.MakeRequest<MfaEnrollResponse>(HttpMethod.Post, $"{Url}/factors", body, CreateAuthedRequestHeaders(jwt));
		}

		/// <inheritdoc />
		public Task<MfaChallengeResponse?> Challenge(string jwt, MfaChallengeParams mfaChallengeParams)
		{
			return Helpers.MakeRequest<MfaChallengeResponse>(HttpMethod.Post, $"{Url}/factors/{mfaChallengeParams.FactorId}/challenge", null, CreateAuthedRequestHeaders(jwt));
		}

		/// <inheritdoc />
		public Task<MfaVerifyResponse?> Verify(string jwt, MfaVerifyParams mfaVerifyParams)
		{
			var body = new Dictionary<string, object>
			{
				{ "code", mfaVerifyParams.Code },
				{ "challenge_id", mfaVerifyParams.ChallengeId }
			};

			return Helpers.MakeRequest<MfaVerifyResponse>(HttpMethod.Post, $"{Url}/factors/{mfaVerifyParams.FactorId}/verify", body, CreateAuthedRequestHeaders(jwt));
		}

		/// <inheritdoc />
		public Task<MfaUnenrollResponse?> Unenroll(string jwt, MfaUnenrollParams mfaUnenrollParams)
		{
			return Helpers.MakeRequest<MfaUnenrollResponse>(HttpMethod.Delete, $"{Url}/factors/{mfaUnenrollParams.FactorId}", null, CreateAuthedRequestHeaders(jwt));
		}

		/// <inheritdoc />
		public Task<BaseResponse> ListFactors(string jwt, MfaAdminListFactorsParams listFactorsParams)
		{
			return Helpers.MakeRequest(HttpMethod.Get, $"{Url}/admin/users/{listFactorsParams.UserId}/factors", null, CreateAuthedRequestHeaders(jwt));
		}

		/// <inheritdoc />
		public Task<MfaAdminDeleteFactorResponse?> DeleteFactor(string jwt, MfaAdminDeleteFactorParams deleteFactorParams)
		{
			return Helpers.MakeRequest<MfaAdminDeleteFactorResponse>(HttpMethod.Delete, $"{Url}/admin/users/{deleteFactorParams.UserId}/factors/{deleteFactorParams.Id}", null, CreateAuthedRequestHeaders(jwt));
		}

		/// <inheritdoc />
		public async Task<ProviderAuthState> LinkIdentity(string token, Provider provider, SignInOptions options)
		{
			var state = Helpers.GetUrlForProvider($"{Url}/user/identities/authorize", provider, options);
			await Helpers.MakeRequest(HttpMethod.Get, state.Uri.ToString(), null, CreateAuthedRequestHeaders(token));
			return state;
		}

		/// <inheritdoc />
		public async Task<bool> UnlinkIdentity(string token, UserIdentity userIdentity)
		{
			var result = await Helpers.MakeRequest(HttpMethod.Delete, $"{Url}/user/identities/${userIdentity.IdentityId}", null, CreateAuthedRequestHeaders(token));
			return result.ResponseMessage is { IsSuccessStatusCode: true };
		}

		/// <summary>
		/// Removes a logged-in session.
		/// </summary>
		/// <param name="jwt"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public Task<BaseResponse> SignOut(string jwt, SignOutScope scope = SignOutScope.Global)
		{
			var data = new Dictionary<string, string>();

			return Helpers.MakeRequest(HttpMethod.Post, $"{Url}/logout?scope={Core.Helpers.GetMappedToAttr(scope).Mapping}", data, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Gets User Details
		/// </summary>
		/// <param name="jwt"></param>
		/// <returns></returns>
		public Task<User?> GetUser(string jwt)
		{
			var data = new Dictionary<string, string>();

			return Helpers.MakeRequest<User>(HttpMethod.Get, $"{Url}/user", data, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Get User details by Id
		/// </summary>
		/// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
		/// <param name="userId">userID</param>
		/// <returns></returns>
		public Task<User?> GetUserById(string jwt, string userId)
		{
			var data = new Dictionary<string, string>();

			return Helpers.MakeRequest<User>(HttpMethod.Get, $"{Url}/admin/users/{userId}", data, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Updates the User data
		/// </summary>
		/// <param name="jwt"></param>
		/// <param name="attributes"></param>
		/// <returns></returns>
		public Task<User?> UpdateUser(string jwt, UserAttributes attributes)
		{
			return Helpers.MakeRequest<User>(HttpMethod.Put, $"{Url}/user", attributes, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Lists users
		/// </summary>
		/// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
		/// <param name="filter">A string for example part of the email</param>
		/// <param name="sortBy">Snake case string of the given key, currently only created_at is supported</param>
		/// <param name="sortOrder">asc or desc, if null desc is used</param>
		/// <param name="page">page to show for pagination</param>
		/// <param name="perPage">items per page for pagination</param>
		/// <returns></returns>
		public Task<UserList<User>?> ListUsers(string jwt, string? filter = null, string? sortBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? perPage = null)
		{
			var data = TransformListUsersParams(filter, sortBy, sortOrder, page, perPage);

			return Helpers.MakeRequest<UserList<User>>(HttpMethod.Get, $"{Url}/admin/users", data, CreateAuthedRequestHeaders(jwt));
		}

		private Dictionary<string, string> TransformListUsersParams(string? filter = null, string? sortBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? perPage = null)
		{
			var query = new Dictionary<string, string>();

			if (filter != null && !string.IsNullOrWhiteSpace(filter))
			{
				query.Add("filter", filter);
			}

			if (!string.IsNullOrWhiteSpace(sortBy))
			{
				var mapTo = Core.Helpers.GetMappedToAttr(sortOrder);
				query.Add("sort", $"{sortBy} {mapTo.Mapping}");
			}

			if (page.HasValue)
			{
				query.Add("page", page.Value.ToString());
			}

			if (perPage.HasValue)
			{
				query.Add("per_page", perPage.Value.ToString());
			}

			return query;
		}

		/// <summary>
		/// Create a user
		/// </summary>
		/// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
		/// <param name="attributes">Additional administrative details</param>
		/// <returns></returns>
		public Task<User?> CreateUser(string jwt, AdminUserAttributes? attributes = null)
		{
			attributes ??= new AdminUserAttributes();

			return Helpers.MakeRequest<User>(HttpMethod.Post, $"{Url}/admin/users", attributes, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Update user by Id
		/// </summary>
		/// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
		/// <param name="userId">userID</param>
		/// <param name="userData">User attributes e.g. email, password, etc.</param>
		/// <returns></returns>
		public Task<User?> UpdateUserById(string jwt, string userId, UserAttributes userData)
		{
			return Helpers.MakeRequest<User>(HttpMethod.Put, $"{Url}/admin/users/{userId}", userData, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Sends a re-authentication request, used for password changes.
		///
		/// See: https://github.com/supabase/gotrue#get-reauthenticate
		/// </summary>
		/// <param name="userJwt">The user's auth token.</param>
		/// <returns></returns>
		public Task<BaseResponse> Reauthenticate(string userJwt)
		{
			return Helpers.MakeRequest(HttpMethod.Get, $"{Url}/reauthenticate", null, CreateAuthedRequestHeaders(userJwt));
		}

		/// <summary>
		/// Delete a user
		/// </summary>
		/// <param name="uid">The user uid you want to remove.</param>
		/// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
		/// <returns></returns>
		public Task<BaseResponse> DeleteUser(string uid, string jwt)
		{
			var data = new Dictionary<string, string>();
			return Helpers.MakeRequest(HttpMethod.Delete, $"{Url}/admin/users/{uid}", data, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Calls the GoTrue server to get the settings (for example, if email auto confirmation is turned on)
		/// </summary>
		/// <returns>mpose up -d
		/// </returns>
		public Task<Settings?> Settings()
		{
			return Helpers.MakeRequest<Settings>(HttpMethod.Get, $"{Url}/settings", null, Headers);
		}

		/// <summary>
		/// Generates email links and OTPs to be sent via a custom email provider.
		/// </summary>
		/// <param name="jwt"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public Task<BaseResponse> GenerateLink(string jwt, GenerateLinkOptions options)
		{
			var url = string.IsNullOrEmpty(options.RedirectTo) ? $"{Url}/admin/generate_link" : $"{Url}/admin/generate_link?redirect_to={options.RedirectTo}";

			return Helpers.MakeRequest(HttpMethod.Post, url, options, CreateAuthedRequestHeaders(jwt));
		}

		/// <summary>
		/// Generates a new Session given a user's access token and refresh token.
		/// </summary>
		/// <param name="refreshToken"></param>
		/// <param name="accessToken"></param>
		/// <returns></returns>
		public Task<Session?> RefreshAccessToken(string accessToken, string refreshToken)
		{
			var headers = new Dictionary<string, string>
			{
				{ "Authorization", $"Bearer {accessToken}" },
			};

			var data = new Dictionary<string, string>
			{
				{ "refresh_token", refreshToken }
			};

			return Helpers.MakeRequest<Session>(HttpMethod.Post, $"{Url}/token?grant_type=refresh_token", data, Headers.MergeLeft(headers));
		}
	}
}

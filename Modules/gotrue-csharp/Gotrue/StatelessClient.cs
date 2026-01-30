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

namespace Supabase.Gotrue
{
	/// <inheritdoc />
	public class StatelessClient : IGotrueStatelessClient<User, Session>
	{
		/// <inheritdoc />
		public async Task<Settings?> Settings(StatelessClientOptions options)
		{
			var api = GetApi(options);
			return await api.Settings();
		}

		/// <inheritdoc />
		public async Task<MfaEnrollResponse?> Enroll(string jwt, MfaEnrollParams mfaEnrollParams, StatelessClientOptions options)
		{
			return await GetApi(options).Enroll(jwt, mfaEnrollParams);
		}

		/// <inheritdoc />
		public async Task<MfaChallengeResponse?> Challenge(string jwt, MfaChallengeParams mfaChallengeParams, StatelessClientOptions options)
		{
			return await GetApi(options).Challenge(jwt, mfaChallengeParams);
		}

		/// <inheritdoc />
		public async Task<MfaVerifyResponse?> Verify(string jwt, MfaVerifyParams mfaVerifyParams, StatelessClientOptions options)
		{
			return await GetApi(options).Verify(jwt, mfaVerifyParams);
		}

		/// <inheritdoc />
		public async Task<MfaVerifyResponse?> ChallengeAndVerify(string jwt, MfaChallengeAndVerifyParams mfaChallengeAndVerifyParams, StatelessClientOptions options)
		{
			var api = GetApi(options);
			var challengeResponse = await api.Challenge(jwt, new MfaChallengeParams
			{
				FactorId = mfaChallengeAndVerifyParams.FactorId
			});

			if (challengeResponse != null)
			{
				var verifyResponse = await api.Verify(jwt, new MfaVerifyParams
				{
					FactorId = mfaChallengeAndVerifyParams.FactorId,
					ChallengeId = challengeResponse.Id,
					Code = mfaChallengeAndVerifyParams.Code
				});

				return verifyResponse;
			}

			return null;
		}

		/// <inheritdoc />
		public async Task<MfaUnenrollResponse?> Unenroll(string jwt, MfaUnenrollParams mfaUnenrollParams, StatelessClientOptions options)
		{
			return await GetApi(options).Unenroll(jwt, mfaUnenrollParams);
		}

		/// <inheritdoc />
		public async Task<MfaListFactorsResponse?> ListFactors(string jwt, StatelessClientOptions options)
		{
			var api = GetApi(options);
			var user = await api.GetUser(jwt);

			if (user != null)
			{
				var response = new MfaListFactorsResponse()
				{
					All = user.Factors,
					Totp = user.Factors.Where(x => x.FactorType == "totp" && x.Status == "verified").ToList()
				};

				return response;
			}

			return null;
		}
		public async Task<MfaGetAuthenticatorAssuranceLevelResponse?> GetAuthenticatorAssuranceLevel(string jwt, StatelessClientOptions options)
		{
			var api = GetApi(options);
			var user = await api.GetUser(jwt);

			if (user != null)
			{
				var payload = new JwtSecurityTokenHandler().ReadJwtToken(jwt).Payload;

				if (payload == null || payload.ValidTo == DateTime.MinValue)
					throw new Exception("`accessToken`'s payload was of an unknown structure.");

				AuthenticatorAssuranceLevel? currentLevel = null;

				if (payload.ContainsKey("aal"))
				{
					currentLevel = Enum.TryParse(payload["aal"].ToString(), out AuthenticatorAssuranceLevel parsedLevel) ? parsedLevel : (AuthenticatorAssuranceLevel?)null;
				}

				AuthenticatorAssuranceLevel? nextLevel = currentLevel;

				var verifiedFactors = user.Factors?.Where(factor => factor.Status == "verified").ToList() ?? new List<Factor>();
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

				return response;
			}

			return null;
		}

		/// <inheritdoc />
		public IGotrueApi<User, Session> GetApi(StatelessClientOptions options) => new Api(options.Url, options.Headers);
		
		/// <inheritdoc />
		public Task<Session?> SignUp(string email, string password, StatelessClientOptions options, SignUpOptions? signUpOptions = null) => SignUp(SignUpType.Email, email, password, options, signUpOptions);
		
		/// <inheritdoc />
		public async Task<Session?> SignUp(SignUpType type, string identifier, string password, StatelessClientOptions options, SignUpOptions? signUpOptions = null)
		{
			var api = GetApi(options);
			var session = type switch
			{
				SignUpType.Email => await api.SignUpWithEmail(identifier, password, signUpOptions),
				SignUpType.Phone => await api.SignUpWithPhone(identifier, password, signUpOptions),
				_ => null
			};

			if (session?.User?.ConfirmedAt != null || session?.User != null && options.AllowUnconfirmedUserSessions)
			{
				return session;
			}

			return null;
		}
		
		/// <inheritdoc />
		public async Task<bool> SignIn(string email, StatelessClientOptions options, SignInOptions? signInOptions = null)
		{
			await GetApi(options).SendMagicLinkEmail(email, signInOptions);
			return true;
		}
		
		/// <inheritdoc />
		public Task<bool> SendMagicLink(string email, StatelessClientOptions options, SignInOptions? signInOptions = null) => SignIn(email, options, signInOptions);
		
		/// <inheritdoc />
		public Task<Session?> SignIn(string email, string password, StatelessClientOptions options) => SignIn(SignInType.Email, email, password, options);
		
		/// <inheritdoc />
		public async Task<Session?> SignIn(SignInType type, string identifierOrToken, string? password = null, StatelessClientOptions? options = null)
		{
			options ??= new StatelessClientOptions();

			var api = GetApi(options);
			Session? session;
			switch (type)
			{
				case SignInType.Email:
					session = await api.SignInWithEmail(identifierOrToken, password!);
					break;
				case SignInType.Phone:
					if (string.IsNullOrEmpty(password))
					{
						await api.SendMobileOTP(identifierOrToken);
						return null;
					}

					session = await api.SignInWithPhone(identifierOrToken, password!);
					break;
				default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}

			if (session?.User?.ConfirmedAt != null || session?.User != null && options.AllowUnconfirmedUserSessions)
				return session;

			return null;
		}

		/// <inheritdoc />
		public ProviderAuthState SignIn(Provider provider, StatelessClientOptions options, SignInOptions? signInOptions = null) => GetApi(options).GetUriForProvider(provider, signInOptions);

		/// <inheritdoc />
		public async Task<bool> SignOut(string accessToken, StatelessClientOptions options)
		{
			var result = await GetApi(options).SignOut(accessToken);
			result.ResponseMessage?.EnsureSuccessStatusCode();
			return true;
		}
		
		/// <inheritdoc />
		public async Task<Session?> VerifyOTP(string phone, string otpToken, StatelessClientOptions options, MobileOtpType type = MobileOtpType.SMS)
		{
			var session = await GetApi(options).VerifyMobileOTP(phone, otpToken, type);

			if (session?.AccessToken != null)
			{
				return session;
			}

			return null;
		}

		/// <inheritdoc />
		public async Task<Session?> VerifyOTP(string email, string otpToken, StatelessClientOptions options, EmailOtpType type = EmailOtpType.MagicLink)
		{
			var session = await GetApi(options).VerifyEmailOTP(email, otpToken, type);

			if (session?.AccessToken != null)
			{
				return session;
			}

			return null;
		}

		/// <inheritdoc />
		public async Task<Session?> VerifyTokenHash(string tokenHash, StatelessClientOptions options, EmailOtpType type = EmailOtpType.Email)
		{
			var session = await GetApi(options).VerifyTokenHash(tokenHash, type);

			if (session?.AccessToken != null)
			{
				return session;
			}

			return null;
		}

		/// <inheritdoc />
		public async Task<User?> Update(string accessToken, UserAttributes attributes, StatelessClientOptions options)
		{
			var result = await GetApi(options).UpdateUser(accessToken, attributes);
			return result;
		}

		/// <inheritdoc />
		public async Task<bool> InviteUserByEmail(string email, string serviceRoleToken, StatelessClientOptions options, InviteUserByEmailOptions? invitationOptions = null)
		{
			var response = await GetApi(options).InviteUserByEmail(email, serviceRoleToken, invitationOptions);
			response.ResponseMessage?.EnsureSuccessStatusCode();
			return true;
		}

		/// <inheritdoc />
		public async Task<bool> ResetPasswordForEmail(string email, StatelessClientOptions options)
		{
			var result = await GetApi(options).ResetPasswordForEmail(email);
			result.ResponseMessage?.EnsureSuccessStatusCode();
			return true;
		}

		/// <inheritdoc />
		public async Task<UserList<User>?> ListUsers(string serviceRoleToken, StatelessClientOptions options, string? filter = null, string? sortBy = null, SortOrder sortOrder = SortOrder.Descending,
			int? page = null, int? perPage = null)
		{
			return await GetApi(options).ListUsers(serviceRoleToken, filter, sortBy, sortOrder, page, perPage);
		}

		/// <inheritdoc />
		public async Task<User?> GetUserById(string serviceRoleToken, StatelessClientOptions options, string userId)
		{
			return await GetApi(options).GetUserById(serviceRoleToken, userId);
		}

		/// <inheritdoc />
		public async Task<User?> GetUser(string serviceRoleToken, StatelessClientOptions options)
		{
			return await GetApi(options).GetUser(serviceRoleToken);
		}

		/// <inheritdoc />
		public Task<User?> CreateUser(string serviceRoleToken, StatelessClientOptions options, string email, string password, AdminUserAttributes? attributes = null)
		{
			attributes ??= new AdminUserAttributes();
			attributes.Email = email;
			attributes.Password = password;

			return CreateUser(serviceRoleToken, options, attributes);
		}

		/// <inheritdoc />
		public async Task<User?> CreateUser(string serviceRoleToken, StatelessClientOptions options, AdminUserAttributes attributes)
		{
			return await GetApi(options).CreateUser(serviceRoleToken, attributes);
		}

		/// <inheritdoc />
		public async Task<User?> UpdateUserById(string serviceRoleToken, StatelessClientOptions options, string userId, AdminUserAttributes userData)
		{
			return await GetApi(options).UpdateUserById(serviceRoleToken, userId, userData);
		}

		/// <inheritdoc />
		public async Task<bool> DeleteUser(string uid, string serviceRoleToken, StatelessClientOptions options)
		{
			var result = await GetApi(options).DeleteUser(uid, serviceRoleToken);
			result.ResponseMessage?.EnsureSuccessStatusCode();
			return true;
		}

		/// <inheritdoc />
		public async Task<Session?> ExchangeCodeForSession(string codeVerifier, string authCode, StatelessClientOptions options)
		{
			return await GetApi(options).ExchangeCodeForSession(codeVerifier, authCode);
		}

		/// <inheritdoc />
		public async Task<Session?> GetSessionFromUrl(Uri uri, StatelessClientOptions options)
		{
			var query = HttpUtility.ParseQueryString(uri.Query);

			var errorDescription = query.Get("error_description");

			if (!string.IsNullOrEmpty(errorDescription))
				throw new Exception(errorDescription);

			var accessToken = query.Get("access_token");

			if (string.IsNullOrEmpty(accessToken))
				throw new Exception("No access_token detected.");

			var expiresIn = query.Get("expires_in");

			if (string.IsNullOrEmpty(expiresIn))
				throw new Exception("No expires_in detected.");

			var refreshToken = query.Get("refresh_token");

			if (string.IsNullOrEmpty(refreshToken))
				throw new Exception("No refresh_token detected.");

			var tokenType = query.Get("token_type");

			if (string.IsNullOrEmpty(tokenType))
				throw new Exception("No token_type detected.");

			var user = await GetApi(options).GetUser(accessToken);

			var session = new Session
			{
				AccessToken = accessToken,
				ExpiresIn = long.Parse(expiresIn),
				RefreshToken = refreshToken,
				TokenType = tokenType,
				User = user
			};

			return session;
		}

		/// <inheritdoc />
		public async Task<Session?> RefreshToken(string accessToken, string refreshToken, StatelessClientOptions options) => 
			await GetApi(options).RefreshAccessToken(accessToken, refreshToken);

		/// <summary>
		/// Class representation options available to the <see cref="Client"/>.
		/// </summary>
		public class StatelessClientOptions
		{
			/// <summary>
			/// Gotrue Endpoint
			/// </summary>
			public string Url { get; set; } = GOTRUE_URL;

			/// <summary>
			/// Headers to be sent with subsequent requests.
			/// </summary>
			public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

			/// <summary>
			/// Very unlikely this flag needs to be changed except in very specific contexts.
			/// 
			/// Enables tests to be E2E tests to be run without requiring users to have
			/// confirmed emails - mirrors the Gotrue server's configuration.
			/// </summary>
			public bool AllowUnconfirmedUserSessions { get; set; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using static GotrueTests.TestUtils;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using static Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;
using static Supabase.Gotrue.Constants.AuthState;

namespace GotrueTests
{
	[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
	[TestClass]
	public class AnonKeyClientTests
	{

		private readonly List<Constants.AuthState> _stateChanges = new List<Constants.AuthState>();

		private IGotrueClient<User, Session> _client;

		private TestSessionPersistence _persistence;

		private void AuthStateListener(IGotrueClient<User, Session> sender, Constants.AuthState newState)
		{
			if (_stateChanges.Contains(newState))
				Debug.WriteLine($"State updated twice {newState}");

			_stateChanges.Add(newState);
		}

		private bool AuthStateIsEmpty()
		{
			return _stateChanges.Count == 0;
		}

		[TestInitialize]
		public void TestInitializer()
		{
			_persistence = new TestSessionPersistence();
			_client = TestUtils.Client();
			_client.SetPersistence(_persistence);
			_client.AddDebugListener(LogDebug);
			_client.AddStateChangedListener(AuthStateListener);
		}

		private void VerifyGoodSession(Session session)
		{
			Contains(_stateChanges, SignedIn);
			AreEqual(_client.CurrentSession, _persistence.SavedSession);
			AreEqual(_client.CurrentUser.Id, session.User.Id);
			IsNotNull(session.AccessToken);
			IsNotNull(session.RefreshToken);
			IsNotNull(session.User);
		}

		private void VerifySignedOut()
		{
			Contains(_stateChanges, SignedOut);
			IsNull(_persistence.SavedSession);
			IsNull(_client.CurrentSession);
			IsNull(_client.CurrentUser);
		}

		[TestMethod("Client: Sign Up User")]
		public async Task SignUpUserEmail()
		{
			IsTrue(AuthStateIsEmpty());

			var email = $"{RandomString(12)}@supabase.io";
			var session = await _client.SignUp(email, PASSWORD);

			VerifyGoodSession(session);
		}

		[TestMethod("Client: Load User From Persistence")]
		public async Task SaveAndLoadUser()
		{
			IsTrue(AuthStateIsEmpty());

			var email = $"{RandomString(12)}@supabase.io";
			var session = await _client.SignUp(email, PASSWORD);

			VerifyGoodSession(session);

			var newPersistence = new TestSessionPersistence();
			newPersistence.SaveSession(session);
			IGotrueClient<User, Session> newClient = new Client(new ClientOptions { AllowUnconfirmedUserSessions = true, Url = "http://127.0.0.1:54321/auth/v1" });
			newClient.SetPersistence(newPersistence);
			newClient.AddDebugListener(LogDebug);
			newClient.AddStateChangedListener(AuthStateListener);

			// Loads the session from storage
			newClient.LoadSession();

			Contains(_stateChanges, SignedIn);
			AreEqual(newClient.CurrentSession, newPersistence.SavedSession);
			IsNotNull(newClient.CurrentSession.AccessToken);
			IsNotNull(newClient.CurrentSession.RefreshToken);
			IsNotNull(newClient.CurrentSession.User);
			VerifyGoodSession(newClient.CurrentSession);

			// Refresh the session
			var refreshedSession = await newClient.RetrieveSessionAsync();

			VerifyGoodSession(refreshedSession);
		}

		[TestMethod("Client: Sign up Phone")]
		public async Task SignUpUserPhone()
		{
			IsTrue(AuthStateIsEmpty());

			var phone1 = GetRandomPhoneNumber();
			var session = await _client.SignUp(Constants.SignUpType.Phone, phone1, PASSWORD, new SignUpOptions { Data = new Dictionary<string, object> { { "firstName", "Testing" } } });

			VerifyGoodSession(session);

			AreEqual("Testing", session.User.UserMetadata["firstName"]);
		}

		[TestMethod("Client: Triggers Token Refreshed Event")]
		public async Task ClientTriggersTokenRefreshedEvent()
		{
			var tsc = new TaskCompletionSource<string>();

			var email = $"{RandomString(12)}@supabase.io";

			IsTrue(AuthStateIsEmpty());

			var session = await _client.SignUp(email, PASSWORD);

			VerifyGoodSession(session);

			_client.AddStateChangedListener((_, args) =>
			{
				if (args == TokenRefreshed)
				{
					tsc.SetResult(_client.CurrentSession.AccessToken);
				}
			});

			_stateChanges.Clear();

			await _client.RefreshSession();
			Contains(_stateChanges, TokenRefreshed);
			AreEqual(_client.CurrentSession, _persistence.SavedSession);

			var newToken = await tsc.Task;
			IsNotNull(newToken);
			AreNotEqual(session.RefreshToken, _client.CurrentSession.RefreshToken);
		}

		[TestMethod("Client: Signs In User (Email, Phone, Refresh token)")]
		public async Task ClientSignsIn()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var emailSession = await _client.SignUp(email, PASSWORD);

			VerifyGoodSession(emailSession);
			_stateChanges.Clear();

			await _client.SignOut();

			VerifySignedOut();

			_stateChanges.Clear();

			var session2 = await _client.SignIn(email, PASSWORD);

			VerifyGoodSession(session2);

			_stateChanges.Clear();

			// Phones
			var phone = GetRandomPhoneNumber();
			var phoneSession = await _client.SignUp(Constants.SignUpType.Phone, phone, PASSWORD);

			VerifyGoodSession(phoneSession);

			_stateChanges.Clear();

			await _client.SignOut();

			VerifySignedOut();

			_stateChanges.Clear();

			emailSession = await _client.SignIn(Constants.SignInType.Phone, phone, PASSWORD);

			VerifyGoodSession(emailSession);
			_stateChanges.Clear();

			// Refresh Token
			var refreshToken = emailSession.RefreshToken;

			var newSession = await _client.SignIn(Constants.SignInType.RefreshToken, refreshToken ?? throw new InvalidOperationException());
			AreEqual(_client.CurrentSession, _persistence.SavedSession);
			Contains(_stateChanges, TokenRefreshed);
			DoesNotContain(_stateChanges, SignedIn);

			IsNotNull(newSession.AccessToken);
			IsNotNull(newSession.RefreshToken);
			IsNotNull(newSession.User);
		}

		[TestMethod("Client: Signs In Anon User")]
		public async Task ClientSignsInAnonymously()
		{
			var options = new SignInAnonymouslyOptions()
			{
				Data = new Dictionary<string, object>() { { "first_name", "John" } }
			};

			var session = await _client.SignInAnonymously(options);

			IsNotNull(session);
			IsTrue(session.User.IsAnonymous);
			AreEqual("John", session.User.UserMetadata["first_name"]);
		}

		[TestMethod("Client: Sends Magic Login Email")]
		public async Task ClientSendsMagicLoginEmail()
		{
			var user = $"{RandomString(12)}@supabase.io";
			var session = await _client.SignUp(user, PASSWORD);

			VerifyGoodSession(session);
			_stateChanges.Clear();

			await _client.SignOut();

			VerifySignedOut();

			_stateChanges.Clear();

			var result = await _client.SignIn(user);
			IsTrue(result);
			AreEqual(0, _stateChanges.Count);
			AreEqual(_client.CurrentSession, _persistence.SavedSession);
		}

		[TestMethod("Client: Sends Magic Login Email (Alias)")]
		public async Task ClientSendsMagicLoginEmailAlias()
		{
			var user = $"{RandomString(12)}@supabase.io";
			var user2 = $"{RandomString(12)}@supabase.io";
			var session = await _client.SignUp(user, PASSWORD);

			VerifyGoodSession(session);

			_stateChanges.Clear();

			await _client.SignOut();

			VerifySignedOut();

			var result = await _client.SendMagicLink(user);
			var result2 = await _client.SendMagicLink(user2, new SignInOptions { RedirectTo = $"com.{RandomString(12)}.deeplink://login" });

			IsTrue(result);
			IsTrue(result2);
		}

		[TestMethod("Client: Returns Auth Url for Provider")]
		public async Task ClientReturnsAuthUrlForProvider()
		{
			var result1 = await _client.SignIn(Constants.Provider.Google);
			AreEqual("http://127.0.0.1:54321/auth/v1/authorize?provider=google", result1.Uri.ToString());

			var result2 = await _client.SignIn(Constants.Provider.Google, new SignInOptions { Scopes = "special scopes please" });
			AreEqual("http://127.0.0.1:54321/auth/v1/authorize?provider=google&scopes=special+scopes+please", result2.Uri.ToString());
		}

		[TestMethod("Client: Returns Verification Code for Provider")]
		public async Task ClientReturnsPKCEVerifier()
		{
			var result = await _client.SignIn(Constants.Provider.Github, new SignInOptions { FlowType = Constants.OAuthFlowType.PKCE });

			VerifySignedOut();

			IsTrue(!string.IsNullOrEmpty(result.PKCEVerifier));
			IsTrue(result.Uri.Query.Contains("flow_type=pkce"));
			IsTrue(result.Uri.Query.Contains("code_challenge="));
			IsTrue(result.Uri.Query.Contains("code_challenge_method=s256"));
			IsTrue(result.Uri.Query.Contains("provider=github"));
		}

		[TestMethod("Client: Update user")]
		public async Task ClientUpdateUser()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var session = await _client.SignUp(email, PASSWORD);

			VerifyGoodSession(session);

			_stateChanges.Clear();

			var attributes = new UserAttributes { Data = new Dictionary<string, object> { { "hello", "world" } } };
			var result = await _client.Update(attributes);
			IsNotNull(result);
			AreEqual(email, _client.CurrentUser.Email);
			IsNotNull(_client.CurrentUser.UserMetadata);
			Contains(_stateChanges, UserUpdated);
			AreEqual(_client.CurrentSession, _persistence.SavedSession);

			await _client.SignOut();

		}

		[TestMethod("Client: Returns current user")]
		public async Task ClientGetUser()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var newUser = await _client.SignUp(email, PASSWORD);

			AreEqual(email, _client.CurrentUser.Email);

			var userByJWT = await _client.GetUser(newUser.AccessToken ?? throw new InvalidOperationException());
			AreEqual(email, userByJWT.Email);
		}

		[TestMethod("Client: Nulls CurrentUser on SignOut")]
		public async Task ClientGetUserAfterLogOut()
		{
			IsTrue(AuthStateIsEmpty());
			var user = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(user, PASSWORD);
			Contains(_stateChanges, SignedIn);

			_stateChanges.Clear();
			await _client.SignOut();

			VerifySignedOut();
		}

		[TestMethod("Client: Log in with new user after log out")]
		public async Task ClientNewUserAfterLogout()
		{
			IsTrue(AuthStateIsEmpty());
			var user = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(user, PASSWORD);
			Contains(_stateChanges, SignedIn);

			var firstUser = _client.CurrentUser.Id;

			_stateChanges.Clear();
			await _client.SignOut();

			VerifySignedOut();
			_stateChanges.Clear();
			IsTrue(AuthStateIsEmpty());
			var user2 = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(user2, PASSWORD);
			Contains(_stateChanges, SignedIn);

			var secondUser = _client.CurrentUser.Id;
			IsFalse(firstUser.Equals(secondUser));
		}

		[TestMethod("Client: Send Reset Password Email")]
		public async Task ClientSendsResetPasswordForEmail()
		{
			var email = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(email, PASSWORD);
			var result = await _client.ResetPasswordForEmail(email);
			IsTrue(result);
		}

		[TestMethod("Client: Send Reset Password Email (PKCE)")]
		public async Task ClientSendsResetPasswordForEmailPKCE()
		{
			var email = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(email, PASSWORD);
			var options = new ResetPasswordForEmailOptions(email)
			{
				RedirectTo = "http://localhost:3000",
				FlowType = Constants.OAuthFlowType.PKCE
			};

			var result = await _client.ResetPasswordForEmail(options);

			IsFalse(string.IsNullOrEmpty(result.PKCEVerifier));
		}

		[TestMethod("Client: Can Form LinkIdentity (PKCE)")]
		public async Task ClientLinkIdentityPKCE()
		{
			var email = $"{RandomString(12)}@supabase.io";

			await ThrowsExceptionAsync<GotrueException>(async () => await _client.LinkIdentity(Constants.Provider.Github, new SignInOptions
			{
				FlowType = Constants.OAuthFlowType.PKCE
			}));

			await ThrowsExceptionAsync<GotrueException>(async () => await _client.LinkIdentity(Constants.Provider.Github, new SignInOptions()));

			var session = await _client.SignUp(email, PASSWORD);

			var result = await _client.LinkIdentity(Constants.Provider.Github, new SignInOptions
			{
				FlowType = Constants.OAuthFlowType.PKCE
			});

			IsFalse(string.IsNullOrEmpty(result.PKCEVerifier));
		}

		[TestMethod("Client: Get Settings")]
		public async Task Settings()
		{
			await _client.Settings();
		}

		[TestMethod("Client: Can change password")]
		public async Task ClientCanChangePassword()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var newPassword = "IAmANewSecretPassword";
			await _client.SignUp(email, PASSWORD);

			await _client.Update(new UserAttributes()
			{
				Password = newPassword
			});

			await _client.SignOut();
			var user = await _client.SignIn(email, newPassword);

			IsTrue(user != null);
		}

		[TestMethod("Client: Can Set Session")]
		public async Task ClientCanSetSession()
		{
			var email = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(email, PASSWORD);

			IsNotNull(_client.CurrentSession);
			IsFalse(string.IsNullOrEmpty(_client.CurrentSession.AccessToken));
			IsFalse(string.IsNullOrEmpty(_client.CurrentSession.RefreshToken));

			var id = _client.CurrentUser.Id;
			var accessToken = _client.CurrentSession.AccessToken!;
			var refreshToken = _client.CurrentSession.RefreshToken!;

			var email2 = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(email2, PASSWORD);

			AreNotEqual(accessToken, _client.CurrentSession.AccessToken);

			var hasStateChangedTsc = new TaskCompletionSource<bool>();
			_client.AddStateChangedListener((sender, changed) =>
			{
				// Should be raised by `SetSession`
				if (changed == SignedIn)
					hasStateChangedTsc.TrySetResult(true);
			});

			await _client.SetSession(accessToken, refreshToken);

			var hasStateChanged = await hasStateChangedTsc.Task;

			IsTrue(hasStateChanged);
			IsNotNull(_client.CurrentSession);
			IsNotNull(_client.CurrentUser);
			AreEqual(id, _client.CurrentUser.Id);

			// As these are fresh, a new token should not be generated.
			AreEqual(accessToken, _client.CurrentSession.AccessToken);
			AreEqual(refreshToken, _client.CurrentSession.RefreshToken);

			await _client.SetSession(accessToken, refreshToken, forceAccessTokenRefresh: true);
			IsNotNull(_client.CurrentSession);
			IsNotNull(_client.CurrentUser);
			AreEqual(id, _client.CurrentUser.Id);

			// As this is being forced to regenerate, the original should be different than the cached.
			AreNotEqual(refreshToken, _client.CurrentSession.RefreshToken);
		}
		

		[TestMethod("Client: Resend")]
		public async Task Resend()
		{
			var email = $"{RandomString(12)}@supabase.io";
			
			// We testing the contract and that it reaches the server.
			// The server should return 200 even if user doesn't exist (depending on config)
			// or 422/404 if it's strict. Either way, a response from the server validates the implementation.
			try 
			{
				var response = await _client.Resend(new ResendParams
				{
					Email = email,
					Type = ResendParams.ResendType.Signup
				});
				
				IsNotNull(response);
				// If we get a response, the request was correctly formed.
				// We don't necessarily require success for the contract to be valid, 
				// but usually /resend returns 200 for security reasons (don't leak user existence).
				IsTrue(response.ResponseMessage?.IsSuccessStatusCode ?? false);
			}
			catch (GotrueException ex)
			{
				// If we get a 400 validation error, it might be due to server configuration or version differences.
				// But reaching the server proves the client-side implementation of the method and model is valid.
				// However, "signup" should be accepted.
				System.Console.WriteLine($"[DEBUG_LOG] Resend failed with: {ex.Message}");
				throw;
			}
		}
	}
}
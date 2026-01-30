using System;
using System.Collections.Generic;
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
using static Supabase.Gotrue.Exceptions.FailureHint.Reason;

namespace GotrueTests
{
	[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
	[TestClass]
	public class AnonKeyClientFailureTests
	{
		private readonly List<Constants.AuthState> _stateChanges = new List<Constants.AuthState>();

		private IGotrueClient<User, Session> _client;
		private TestSessionPersistence _persistence;

		private void AuthStateListener(IGotrueClient<User, Session> sender, Constants.AuthState newState)
		{
			if (_stateChanges.Contains(newState) && newState != SignedOut)
				throw new ArgumentException($"State updated twice {newState}");

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
			_client = new Client(new ClientOptions { AllowUnconfirmedUserSessions = true, Url = "http://127.0.0.1:54321/auth/v1"});
			_client.SetPersistence(_persistence);
			_client.AddDebugListener(LogDebug);
			_client.AddStateChangedListener(AuthStateListener);
		}

		[TestMethod("Client: Sign Up With Bad Password")]
		public async Task SignUpUserEmailBadPassword()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await _client.SignUp(email, "x");
			});
			AreEqual(UserBadPassword, x.Reason);
			IsNull(_persistence.SavedSession);
			Contains(_stateChanges, SignedOut);
			AreEqual(1, _stateChanges.Count);
		}

		[TestMethod("Client: Sign Up With Bad Email Address")]
		public async Task SignUpUserEmailBadEmailAddress()
		{
			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await _client.SignUp("not a real email address", PASSWORD);
			});
			AreEqual(UserBadEmailAddress, x.Reason);
			IsNull(_persistence.SavedSession);
			Contains(_stateChanges, SignedOut);
			AreEqual(1, _stateChanges.Count);
		}

		[TestMethod("Client: Sign up without a phone number")]
		public async Task SignUpUserPhone()
		{
			IsTrue(AuthStateIsEmpty());

			var phone1 = "";
			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await _client.SignUp(Constants.SignUpType.Phone, phone1, PASSWORD, new SignUpOptions { Data = new Dictionary<string, object> { { "firstName", "Testing" } } });
			});
			AreEqual(UserBadPhoneNumber, x.Reason);
			IsNull(_persistence.SavedSession);
			Contains(_stateChanges, SignedOut);
			AreEqual(1, _stateChanges.Count);
		}

		[TestMethod("Client: Signs Up the same user twice")]
		public async Task SignsUpUserTwiceShouldReturnBadRequest()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var session = await _client.SignUp(email, PASSWORD);

			IsNotNull(session);

			Contains(_stateChanges, SignedIn);
			AreEqual(_client.CurrentSession, _persistence.SavedSession);
			_stateChanges.Clear();

			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await _client.SignUp(email, PASSWORD);
			});

			AreEqual(UserAlreadyRegistered, x.Reason);
			IsNull(_persistence.SavedSession);
			Contains(_stateChanges, SignedOut);
			AreEqual(1, _stateChanges.Count);
		}

		[TestMethod("Client: Bogus refresh token")]
		public async Task ClientTriggersTokenRefreshedEvent()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var user = await _client.SignUp(email, PASSWORD);
			IsNotNull(user);

			_client.CurrentSession.RefreshToken = "bogus token";

			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await _client.RefreshSession();
			});
			AreEqual(InvalidRefreshToken, x.Reason);
		}

		[TestMethod("Client: expired token")]
		public async Task ExpiredTokenTest()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var emailSession = await _client.SignUp(email, PASSWORD);

			IsNotNull(emailSession.AccessToken);
			IsNotNull(emailSession.RefreshToken);
			IsNotNull(emailSession.User);

			await _client.RefreshSession();
			
			IsNotNull(emailSession.AccessToken);
			IsNotNull(emailSession.RefreshToken);
			IsNotNull(emailSession.User);

			_client.CurrentSession.CreatedAt = DateTime.UtcNow.AddDays(-10);
			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await _client.RefreshSession();
			});
			AreEqual(ExpiredRefreshToken, x.Reason);
		}

		[TestMethod("Client: Send Reset Password Email for unknown email")]
		public async Task ClientSendsResetPasswordForEmail()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var result = await _client.ResetPasswordForEmail(email);
			IsTrue(result);
		}


		[TestMethod("Client: Throws Exception on Invalid Username and Password")]
		public async Task ClientSignsInUserWrongPassword()
		{
			var user = $"{RandomString(12)}@supabase.io";
			await _client.SignUp(user, PASSWORD);
			await _client.SignOut();

			await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				var result = await _client.SignIn(user, PASSWORD + "$");
				IsNotNull(result);
			});
		}
	}

}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using static Supabase.Gotrue.Constants;
using static GotrueTests.TestUtils;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace GotrueTests
{
	[TestClass]
	[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
	public class ServiceRoleTests
	{
		private IGotrueAdminClient<User> _client;

		private readonly string _serviceKey = GenerateServiceRoleToken();

		[TestInitialize]
		public void TestInitializer()
		{
			_client = TestUtils.AdminClient();
		}

		[TestMethod("Service Role: Update User")]
		public async Task UpdateUser()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var session = await _client.CreateUser(email, PASSWORD);
			IsNotNull(session);
			IsNotNull(session.Id);

			var result2 = await _client.UpdateUserById(session.Id, new AdminUserAttributes { UserMetadata = new Dictionary<string, object> { { "hello", "updated" } } });

			AreEqual("updated", result2.UserMetadata["hello"]);

			var result3 = await _client.GetUserById(session.Id);
			AreEqual("updated", result3.UserMetadata["hello"]);
		}

		[TestMethod("Service Role: Send Invite Email")]
		public async Task SendsInviteEmail()
		{
			var user = $"{RandomString(12)}@supabase.io";
			var result = await _client.InviteUserByEmail(user);
			IsTrue(result);
		}

		[TestMethod("Service Role: List users")]
		public async Task ListUsers()
		{
			var result = await _client.ListUsers();
			IsTrue(result.Users.Count > 0);
		}

		[TestMethod("Service Role: List users by page")]
		public async Task ListUsersPagination()
		{
			var page1 = await _client.ListUsers(page: 1, perPage: 1);
			var page2 = await _client.ListUsers(page: 2, perPage: 1);

			AreEqual(page1.Users.Count, 1);
			AreEqual(page2.Users.Count, 1);
			AreNotEqual(page1.Users[0].Id, page2.Users[0].Id);
		}

		[TestMethod("Service Role: Lists users sort")]
		public async Task ListUsersSort()
		{
			var result1 = await _client.ListUsers(sortBy: "created_at", sortOrder: SortOrder.Ascending);
			var result2 = await _client.ListUsers(sortBy: "created_at", sortOrder: SortOrder.Descending);

			AreNotEqual(result1.Users[0].Id, result2.Users[0].Id);
		}

		[TestMethod("Service role: Lists users with filter")]
		public async Task ListUsersFilter()
		{
			var user = $"{RandomString(12)}@supabase.io";
			var result = await _client.CreateUser(user, PASSWORD);
			IsNotNull(result);

			// ReSharper disable once StringLiteralTypo
			var result1 = await _client.ListUsers(filter: "@nonexistingrandomemailprovider.com");
			var result2 = await _client.ListUsers(filter: "@supabase.io");

			AreNotEqual(result2.Users.Count, 0);
			AreEqual(result1.Users.Count, 0);
			AreNotEqual(result1.Users.Count, result2.Users.Count);
		}

		[TestMethod("Service Role: Get User by Id")]
		public async Task GetUserById()
		{
			var result = await _client.ListUsers(page: 1, perPage: 1);

			var userResult = result.Users[0];
			var userByIdResult = await _client.GetUserById(userResult.Id ?? throw new InvalidOperationException());

			AreEqual(userResult.Id, userByIdResult.Id);
			AreEqual(userResult.Email, userByIdResult.Email);
		}

		[TestMethod("Service Role: Create a user")]
		public async Task CreateUser()
		{
			var result = await _client.CreateUser($"{RandomString(12)}@supabase.io", PASSWORD);

			IsNotNull(result);

			var attributes = new AdminUserAttributes
			{
				UserMetadata = new Dictionary<string, object> { { "firstName", "123" } },
				AppMetadata = new Dictionary<string, object> { { "roles", new List<string> { "editor", "publisher" } } }
			};

			var result2 = await _client.CreateUser($"{RandomString(12)}@supabase.io", PASSWORD, attributes);
			AreEqual("123", result2.UserMetadata["firstName"]);

			var result3 = await _client.CreateUser(new AdminUserAttributes { Email = $"{RandomString(12)}@supabase.io", Password = PASSWORD });
			IsNotNull(result3);
		}

		[TestMethod("Service Role: Update User by Id")]
		public async Task UpdateUserById()
		{
			var createdUser = await _client.CreateUser($"{RandomString(12)}@supabase.io", PASSWORD);

			IsNotNull(createdUser);

			var updatedUser = await _client.UpdateUserById(createdUser.Id ?? throw new InvalidOperationException(), new AdminUserAttributes { Email = $"{RandomString(12)}@supabase.io" });

			IsNotNull(updatedUser);

			AreEqual(createdUser.Id, updatedUser.Id);
			AreNotEqual(createdUser.Email, updatedUser.Email);
		}

		[TestMethod("Service Role: Ban User by Id")]
		public async Task BanUserById()
		{
			var createdUser = await _client.CreateUser($"{RandomString(12)}@supabase.io", PASSWORD);

			IsNotNull(createdUser);

			int banDurationSeconds = RandomNumber();
			DateTime bannedUntil = DateTime.UtcNow + TimeSpan.FromSeconds(banDurationSeconds);
			var updatedUser = await _client.UpdateUserById(createdUser.Id ?? throw new InvalidOperationException(), new AdminUserAttributes { BanDuration = $"{banDurationSeconds}s" });

			IsNotNull(updatedUser);

			AreEqual(createdUser.Id, updatedUser.Id);
			IsNotNull(updatedUser.BannedUntil);
			IsTrue((updatedUser.BannedUntil.Value - bannedUntil).Duration().TotalSeconds < 1);

			updatedUser = await _client.UpdateUserById(createdUser.Id ?? throw new InvalidOperationException(), new AdminUserAttributes { BanDuration = "none" });
			IsNotNull(updatedUser);

			AreEqual(createdUser.Id, updatedUser.Id);
			IsFalse(updatedUser.BannedUntil.HasValue);
		}

		[TestMethod("Service Role: Delete User")]
		public async Task DeletesUser()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var user = await _client.CreateUser(email, PASSWORD);
			var uid = user.Id;

			var result = await _client.DeleteUser(uid ?? throw new InvalidOperationException());

			IsTrue(result);
		}
		
		[TestMethod("Service Role: Soft Delete User")]
		public async Task SoftDeletesUser()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var user = await _client.CreateUser(email, PASSWORD);
			var uid = user.Id;

			var result = await _client.DeleteUser(uid ?? throw new InvalidOperationException(), true);
			var deletedUser = await _client.GetUserById(uid);

			IsTrue(result);
			Assert.IsNotNull(deletedUser.DeletedAt);
		}

		[TestMethod("Nonce generation and verification")]
		public void NonceGeneration()
		{
			var nonce = Helpers.GenerateNonce();
			IsNotNull(nonce);
			AreEqual(128, nonce.Length);

			var pkceVerifier = Helpers.GeneratePKCENonceVerifier(nonce);
			IsNotNull(pkceVerifier);
			AreEqual(43, pkceVerifier.Length);

			var appleVerifier = Helpers.GenerateSHA256NonceFromRawNonce(nonce);
			IsNotNull(appleVerifier);
			AreEqual(64, appleVerifier.Length);

			const string helloNonce = "hello_world_nonce";

			var helloPkceVerifier = Helpers.GeneratePKCENonceVerifier(helloNonce);
			IsNotNull(helloPkceVerifier);
			// ReSharper disable once StringLiteralTypo
			AreEqual("9TMmi4JOlYOQEP2Ha39WXj9pySILGnAfQsz-yXws0yE", helloPkceVerifier);

			var helloAppleVerifier = Helpers.GenerateSHA256NonceFromRawNonce(helloNonce);
			IsNotNull(helloAppleVerifier);
			AreEqual("f533268b824e95839010fd876b7f565e3f69c9220b1a701f42ccfec97c2cd321", helloAppleVerifier);
		}

		/*[TestMethod("Service Role: Generate Email Signup Link")]
		public async Task GenerateEmailSignupLink()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var options = new GenerateLinkSignupOptions(email, PASSWORD);

			var result = await _client.GenerateLink(options);

			AreEqual(result.VerificationType, "signup");
		}

		[TestMethod("Service Role: Generate Email Invite Link")]
		public async Task GenerateEmailInviteLink()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var options = new GenerateLinkOptions(GenerateLinkOptions.LinkType.Invite, email);

			var result = await _client.GenerateLink(options);

			AreEqual(result.VerificationType, "invite");
		}*/

		[TestMethod("Service Role: Generate Email Magic Link")]
		public async Task GenerateEmailMagicLink()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var options = new GenerateLinkOptions(GenerateLinkOptions.LinkType.MagicLink, email);

			var result = await _client.GenerateLink(options);

			// Since this is a non-existent user, this is considered a "signup"
			AreEqual(result.VerificationType, "signup");

			var result2 = await _client.GenerateLink(options);

			// When trying again, this should produce a 'magiclink'
			AreEqual(result2.VerificationType, "magiclink");
		}

		[TestMethod("Service Role: Generate Email Recovery Link")]
		public async Task GenerateEmailRecoveryLink()
		{
			var email = $"{RandomString(12)}@supabase.io";

			var options = new GenerateLinkOptions(GenerateLinkOptions.LinkType.MagicLink, email);
			var result = await _client.GenerateLink(options);

			// Since this is a non-existent user, this is considered a "signup"
			AreEqual(result.VerificationType, "signup");

			var options2 = new GenerateLinkOptions(GenerateLinkOptions.LinkType.Recovery, email);
			var result2 = await _client.GenerateLink(options2);
			AreEqual(result2.VerificationType, "recovery");
		}

		[TestMethod("Service Role: Generate Email Change Link")]
		public async Task GenerateEmailChangeLink()
		{
			var email = $"{RandomString(12)}@supabase.io";
			var newEmail = $"{RandomString(12)}@supabase.io";

			await _client.CreateUser(new AdminUserAttributes() { Email = email });

			var options = new GenerateLinkEmailChangeCurrentOptions(email, newEmail);
			var result = await _client.GenerateLink(options);
			AreEqual(result.VerificationType, "email_change_current");

			var options2 = new GenerateLinkEmailChangeNewOptions(email, newEmail);
			var result2 = await _client.GenerateLink(options2);
			AreEqual(result2.VerificationType, "email_change_new");
		}
	}
}
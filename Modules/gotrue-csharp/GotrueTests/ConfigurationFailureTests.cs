using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using static GotrueTests.TestUtils;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using static Supabase.Gotrue.Exceptions.FailureHint.Reason;

namespace GotrueTests
{
	[TestClass]
	public class ConfigurationFailureTests
	{
		[TestMethod("Bad URL message")]
		public async Task BadUrlTest()
		{
			var client = new Client(new ClientOptions { Url = "https://badprojecturl.supabase.co", AllowUnconfirmedUserSessions = true });
			client.AddDebugListener(LogDebug);

			var email = $"{RandomString(12)}@supabase.io";
			GotrueException gte = null;
			try
			{
				await client.SignUp(email, PASSWORD);
			}
			catch (GotrueException e)
			{
				gte = e;
			}
			AreEqual(Offline, gte?.Reason);
		}

		[TestMethod("Bad service key message")]
		public async Task BadServiceApiKeyTest()
		{
			IGotrueAdminClient<User> adminClient = new AdminClient("bad_service_key", new ClientOptions { AllowUnconfirmedUserSessions = true, Url = "http://127.0.0.1:54321/auth/v1" });
			AreEqual(true, ((AdminClient)adminClient).Options.AllowUnconfirmedUserSessions);

			var x = await ThrowsExceptionAsync<GotrueException>(async () =>
			{
				await adminClient.ListUsers();
			});
			
			AreEqual(AdminTokenRequired, x.Reason);
		}
	}
}

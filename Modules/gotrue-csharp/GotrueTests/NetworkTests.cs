using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;

namespace GotrueTests
{
	[TestClass]
	public class NetworkTests
	{

		private const string PASSWORD = "I@M@SuperP@ssWord";

		private readonly List<Constants.AuthState> _stateChanges = new List<Constants.AuthState>();

		private void AuthStateListener(IGotrueClient<User, Session> sender, Constants.AuthState newState)
		{
			if (_stateChanges.Contains(newState))
				Debug.WriteLine($"State updated twice {newState}");

			_stateChanges.Add(newState);
		}

		[TestMethod("Good Ping Check")]
		public async Task GoodPingTest()
		{
			var clientOptions = new ClientOptions { AllowUnconfirmedUserSessions = true, Url = "http://127.0.0.1:54321/auth/v1"};
			var client = new Client(clientOptions);
			client.Online = false;
			var status = new NetworkStatus();

			var url =
				$"{clientOptions.Url}/settings";

			status.Client = client;
			var online = await status.PingCheck(url);
			Assert.IsTrue(online);
			Assert.IsTrue(client.Online);

			client.Online = false;

			await status.StartAsync(url);
			Assert.IsTrue(client.Online);
		}

		[TestMethod("Bad Ping Check")]
		public async Task BadPingTest()
		{
			var client = new Client(new ClientOptions
			{
				AllowUnconfirmedUserSessions = true, Url = "https://badprojecturl.supabase.co"
			});
			client.Online = true;
			var status = new NetworkStatus();
			status.Client = client;
			var online = await status.PingCheck("https://badprojecturl.supabase.co");
			Assert.IsFalse(online);
			Assert.IsFalse(client.Online);
		}

		[TestMethod("Network Not Available On User Sign Up")]
		public async Task BadNetworkChecks()
		{
			var client = new Client(new ClientOptions { AllowUnconfirmedUserSessions = true, Url = "https://badprojecturl.supabase.co" });
			client.AddDebugListener(TestUtils.LogDebug);
			client.AddStateChangedListener(AuthStateListener);
			client.Online = false;

			var email = $"{TestUtils.RandomString(12)}@supabase.io";
			GotrueException x = null;
			try
			{
				await client.SignUp(email, PASSWORD);
			}
			catch (GotrueException gte)
			{
				x = gte;
			}
			Assert.AreEqual(FailureHint.Reason.Offline, x?.Reason);
		}

		[TestMethod("Network Not Available For User Refresh")]
		public async Task BadTokenRefresh()
		{
			var client = new Client(new ClientOptions { AllowUnconfirmedUserSessions = true, Url = "http://127.0.0.1:54321/auth/v1"});
			client.AddDebugListener(TestUtils.LogDebug);
			client.AddStateChangedListener(AuthStateListener);
			client.Online = true;

			var email = $"{TestUtils.RandomString(12)}@supabase.io";
			await client.SignUp(email, PASSWORD);

			client.Online = false;

			GotrueException x = null;
			try
			{
				await client.RefreshToken();
			}
			catch (GotrueException gte)
			{
				x = gte;
			}
			Assert.AreEqual(FailureHint.Reason.Offline, x?.Reason);

			// Now let's go online and give it another try.
			client.Online = true;
			await client.RefreshToken();

			// Let's try shutting down.
			client.Shutdown();
		}
	}
}

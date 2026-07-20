using Microsoft.VisualStudio.TestTools.UnitTesting;
using SupabaseTests.Stubs;

namespace SupabaseTests
{
    /// <summary>
    /// Feature under test: Supabase.Client wires its Realtime client's PostgrestClient to its own Postgrest
    /// client automatically, so models received from Realtime postgres_changes events can call Update()/Delete()
    /// directly without any manual setup.
    /// See: https://github.com/supabase-community/realtime-csharp/issues/35
    /// </summary>
    [TestClass]
    public class ClientPostgrestWiringTests
    {
        [TestMethod(DisplayName = "The DI constructor wires Realtime.Options.PostgrestClient to the injected Postgrest client")]
        public void GivenDIConstructor_RealtimePostgrestClient_IsSetToInjectedPostgrestClient()
        {
            var postgrest = new FakeRestClient();
            var realtime = new FakeRealtimeClient();
            _ = new Supabase.Client(new FakeAuthClient(), realtime, new FakeFunctionsClient(), postgrest, new FakeStorageClient(), new Supabase.SupabaseOptions());
            Assert.AreSame(postgrest, realtime.Options.PostgrestClient);
        }

        [TestMethod(DisplayName = "The URL constructor wires Realtime.Options.PostgrestClient to its own Postgrest client")]
        public void GivenUrlConstructor_RealtimePostgrestClient_IsSetToOwnPostgrestClient()
        {
            var client = new Supabase.Client("http://localhost", "test-key");
            Assert.AreSame(client.Postgrest, client.Realtime.Options.PostgrestClient);
        }
    }
}

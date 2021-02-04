using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase;
using Supabase.Realtime;
using SupabaseTests.Models;
using static Supabase.Client;

namespace SupabaseTests
{
    [TestClass]
    public class Api
    {
        [TestInitialize]
        public async Task InitializeTest()
        {
            await Supabase.Client.Initialize("http://localhost", null, new Supabase.SupabaseOptions
            {
                AuthUrlFormat = "{0}:9999",
                RealtimeUrlFormat = "{0}:4000/socket",
                RestUrlFormat = "{0}:3000"
            });
        }

        [TestMethod("Client: Initializes.")]
        public void ClientInitializes()
        {
            Assert.IsNotNull(Supabase.Client.Instance.Realtime);
            Assert.IsNotNull(Supabase.Client.Instance.Auth);
        }

        [TestMethod("Client: Connects to Realtime")]
        public Task<bool> ClientConnectsToRealtime()
        {
            var tsc = new TaskCompletionSource<bool>();

            Task.Run(async () =>
            {

                await Supabase.Client.Instance.Realtime.Connect();

                var table = Supabase.Client.Instance.From<Models.Channel>();

                await table.On(ChannelEventType.Insert, (object sender, SocketResponseEventArgs args) =>
                {
                    Assert.IsNotNull(args);
                    tsc.SetResult(true);
                });

                await table.Insert(new Models.Channel { Slug = Guid.NewGuid().ToString() });
            });

            return tsc.Task;
        }
    }
}

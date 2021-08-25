using System;
using System.Linq;
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
            await InitializeAsync("http://localhost", null, new Supabase.SupabaseOptions
            {
                AuthUrlFormat = "{0}:9999",
                RealtimeUrlFormat = "{0}:4000/socket",
                RestUrlFormat = "{0}:3000",
                ShouldInitializeRealtime = true,
                AutoConnectRealtime = true
            });
        }

        [TestMethod("Client: Initializes.")]
        public void ClientInitializes()
        {
            Assert.IsNotNull(Supabase.Client.Instance.Realtime);
            Assert.IsNotNull(Supabase.Client.Instance.Auth);
        }

        [TestMethod("Client: Connects to Realtime")]
        public async Task ClientConnectsToRealtime()
        {
            var tsc = new TaskCompletionSource<bool>();

            await Supabase.Client.Instance.Realtime.ConnectAsync();

            var table = Supabase.Client.Instance.From<Models.Channel>();

            await table.On(ChannelEventType.Insert, (object sender, SocketResponseEventArgs args) =>
            {
                tsc.SetResult(args != null);
            });

            await table.Insert(new Models.Channel { Slug = Guid.NewGuid().ToString() });

            var result = await tsc.Task;

            Assert.IsTrue(result);
        }

        [TestMethod("SupabaseModel: Successfully Updates")]
        public async Task SupabaseModelUpdates()
        {
            var model = new Models.Channel { Slug = Guid.NewGuid().ToString() };
            var insertResult = await Supabase.Client.Instance.From<Models.Channel>().Insert(model);
            var newChannel = insertResult.Models.FirstOrDefault();

            var newSlug = $"Updated Slug @ {DateTime.Now.ToLocalTime()}";
            newChannel.Slug = newSlug;

            var updatedResult = await newChannel.Update<Models.Channel>();

            Assert.AreEqual(newSlug, updatedResult.Models.First().Slug);
        }

        [TestMethod("SupabaseModel: Successfully Deletes")]
        public async Task SupabaseModelDeletes()
        {
            var slug = Guid.NewGuid().ToString();
            var model = new Models.Channel { Slug = slug };

            var insertResult = await Supabase.Client.Instance.From<Models.Channel>().Insert(model);
            var newChannel = insertResult.Models.FirstOrDefault();

            await newChannel.Delete<Models.Channel>();

            var result = await Supabase.Client.Instance.From<Models.Channel>().Filter("slug", Postgrest.Constants.Operator.Equals, slug).Get();

            Assert.AreEqual(0, result.Models.Count);
        }
    }
}

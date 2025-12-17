using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using SupabaseTests.Stubs;

namespace SupabaseTests
{
    [TestClass]
    public class Client
    {
        private static readonly Random Random = new();

        private Supabase.Client _instance;

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        [TestInitialize]
        public async Task InitializeTest()
        {
            _instance = new Supabase.Client("http://localhost", "sb_secret_N7UND0UgjKTVK-Uodkm0Hg_xSvEMPvz", new Supabase.SupabaseOptions
            {
                AuthUrlFormat = "{0}:54321/rest/v1",
                RealtimeUrlFormat = "ws://127.0.0.1:54321/realtime/v1",
                RestUrlFormat = "{0}:54321/rest/v1",
                AutoConnectRealtime = false,
            });
            await _instance.InitializeAsync();
        }

        [TestMethod("Client: Initializes.")]
        public void ClientInitializes()
        {
            Assert.IsNotNull(_instance.Realtime);
            Assert.IsNotNull(_instance.Auth);
        }

        [TestMethod("SupabaseModel: Successfully Updates")]
        public async Task SupabaseModelUpdates()
        {
            var model = new Models.Channel { Slug = Guid.NewGuid().ToString() };
            var insertResult = await _instance.From<Models.Channel>().Insert(model);
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

            var insertResult = await _instance.From<Models.Channel>().Insert(model);
            var newChannel = insertResult.Models.FirstOrDefault();

            await newChannel.Delete<Models.Channel>();

            var result = await _instance.From<Models.Channel>()
                .Filter("slug", Constants.Operator.Equals, slug).Get();

            Assert.AreEqual(0, result.Models.Count);
        }

        [TestMethod("Supports Dependency Injection for clients via property")]
        public void SupportsDIForClientsViaProperty()
        {
            _instance.Auth = new FakeAuthClient();
            _instance.Functions = new FakeFunctionsClient();
            _instance.Realtime = new FakeRealtimeClient();
            _instance.Postgrest = new FakeRestClient();
            _instance.Storage = new FakeStorageClient();

            Assert.ThrowsExceptionAsync<NotImplementedException>(() => _instance.Auth.GetUser(""));
            Assert.ThrowsExceptionAsync<NotImplementedException>(() => _instance.Functions.Invoke(""));
            Assert.ThrowsExceptionAsync<NotImplementedException>(() => _instance.Realtime.ConnectAsync());
            Assert.ThrowsExceptionAsync<NotImplementedException>(() => _instance.Postgrest.Rpc("", null));
            Assert.ThrowsExceptionAsync<NotImplementedException>(() => _instance.Storage.ListBuckets());
        }
    }
}
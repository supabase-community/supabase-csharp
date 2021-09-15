using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase;
using Supabase.Realtime;
using Supabase.Storage;
using SupabaseTests.Models;
using static Supabase.Client;

namespace SupabaseTests
{
    [TestClass]
    public class StorageBucket
    {
        private static string SERVICE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoic2VydmljZV9yb2xlIiwiaWF0IjoxNjEzNTMxOTg1LCJleHAiOjE5MjkxMDc5ODV9.th84OKK0Iz8QchDyXZRrojmKSEZ-OuitQm_5DvLiSIc";

        private Supabase.Storage.Client storage => Supabase.Client.Instance.Storage;

        [TestInitialize]
        public async Task InitializeTest()
        {
            await InitializeAsync("http://localhost", null, new Supabase.SupabaseOptions
            {
                AuthUrlFormat = "{0}:9999",
                RealtimeUrlFormat = "{0}:4000/socket",
                RestUrlFormat = "{0}:3000",
                StorageUrlFormat = "{0}:5000",
                ShouldInitializeRealtime = true,
                AutoConnectRealtime = true,
                Headers =
                {
                    { "Authorization", $"Bearer {SERVICE_KEY}" }
                }
            });
        }

        [TestMethod("Bucket: List")]
        public async Task List()
        {
            var buckets = await storage.ListBuckets();

            Assert.IsTrue(buckets.Count > 0);
            Assert.IsInstanceOfType(buckets, typeof(List<Bucket>));
        }

        [TestMethod("Bucket: Get")]
        public async Task Get()
        {
            var bucket = await storage.GetBucket("public-bucket");
            Assert.IsInstanceOfType(bucket, typeof(Bucket));
        }

        [TestMethod("Bucket: Create, Private")]
        public async Task CreatePrivate()
        {
            var id = Guid.NewGuid().ToString();
            var insertId = await storage.CreateBucket(id);

            Assert.AreEqual(id, insertId);

            var bucket = await storage.GetBucket(id);
            Assert.IsFalse(bucket.Public);
        }

        [TestMethod("Bucket: Create, Public")]
        public async Task CreatePublic()
        {
            var id = Guid.NewGuid().ToString();
            await storage.CreateBucket(id, new BucketUpsertOptions { Public = true }); ;

            var bucket = await storage.GetBucket(id);

            Assert.IsTrue(bucket.Public);
        }

        [TestMethod("Bucket: Update")]
        public async Task Update()
        {
            var id = Guid.NewGuid().ToString();
            await storage.CreateBucket(id);

            var privateBucket = await storage.GetBucket(id);
            Assert.IsFalse(privateBucket.Public);

            await storage.UpdateBucket(id, new BucketUpsertOptions { Public = true });

            var nowPublicBucket = await storage.GetBucket(id);
            Assert.IsTrue(nowPublicBucket.Public);
        }

        [TestMethod("Bucket: Empty")]
        public async Task Empty()
        {
            var id = Guid.NewGuid().ToString();
            await storage.CreateBucket(id);

            for (var i = 0; i < 5; i++)
            {
                await storage.From(id).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
            }

            var initialList = await storage.From(id).List();

            Assert.IsTrue(initialList.Count > 0);

            await storage.EmptyBucket(id);

            var listAfterEmpty = await storage.From(id).List();

            Assert.IsTrue(listAfterEmpty.Count == 0);
        }

        [TestMethod("Bucket: Delete, Throws Error if Not Empty")]
        public async Task DeleteThrows()
        {
            var id = Guid.NewGuid().ToString();
            await storage.CreateBucket(id);

            for (var i = 0; i < 5; i++)
            {
                await storage.From(id).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
            }

            await Assert.ThrowsExceptionAsync<BadRequestException>(async () =>
            {
                await storage.DeleteBucket(id);
            });
        }

        [TestMethod("Bucket: Delete")]
        public async Task Delete()
        {
            var id = Guid.NewGuid().ToString();
            await storage.CreateBucket(id);

            for (var i = 0; i < 5; i++)
            {
                await storage.From(id).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
            }

            await storage.EmptyBucket(id);
            await storage.DeleteBucket(id);

            Assert.IsNull(await storage.GetBucket(id));
        }
    }
}

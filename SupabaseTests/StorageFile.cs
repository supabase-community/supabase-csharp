using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
    public class StorageFile
    {
        private static string SERVICE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoic2VydmljZV9yb2xlIiwiaWF0IjoxNjEzNTMxOTg1LCJleHAiOjE5MjkxMDc5ODV9.th84OKK0Iz8QchDyXZRrojmKSEZ-OuitQm_5DvLiSIc";


        private Supabase.Storage.Client storage => Supabase.Client.Instance.Storage;
        private string bucketId;
        private StorageFileApi bucket;

        [TestInitialize]
        public async Task InitializeTest()
        {
            bucketId = Guid.NewGuid().ToString();

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


            if (bucket == null && await storage.GetBucket(bucketId) == null)
            {
                await storage.CreateBucket(bucketId, new BucketUpsertOptions { Public = true });
            }

            bucket = storage.From(bucketId);
        }

        [TestMethod("File: Upload File")]
        public async Task UploadFile()
        {
            var tsc = new TaskCompletionSource<bool>();

            var asset = "supabase-csharp.png";
            var name = $"{Guid.NewGuid()}.png";
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
            var imagePath = Path.Combine(basePath, "Assets", asset);

            UploadProgressChangedEventHandler onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            await bucket.Upload(imagePath, name, null, onProgress);

            var list = await bucket.List();

            var existing = list.Find(item => item.Name == name);
            Assert.IsNotNull(existing);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Upload Arbitrary Byte Array")]
        public async Task UploadArbitraryByteArray()
        {
            var tsc = new TaskCompletionSource<bool>();

            var name = $"{Guid.NewGuid()}.bin";
            UploadProgressChangedEventHandler onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            await bucket.Upload(new Byte[] { 0x0, 0x0, 0x0 }, name, null, onProgress);

            var list = await bucket.List();

            var existing = list.Find(item => item.Name == name);
            Assert.IsNotNull(existing);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Download")]
        public async Task DownloadFile()
        {
            var tsc = new TaskCompletionSource<bool>();

            var asset = "supabase-csharp.png";
            var name = $"{Guid.NewGuid()}.png";
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
            var imagePath = Path.Combine(basePath, "Assets", asset);

            await bucket.Upload(imagePath, name);

            DownloadProgressChangedEventHandler onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            var downloadPath = Path.Combine(basePath, name);
            await bucket.Download(name, downloadPath, onProgress);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            Assert.IsTrue(File.Exists(downloadPath));

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Download Bytes")]
        public async Task DownloadBytes()
        {
            var tsc = new TaskCompletionSource<bool>();

            var data = new Byte[] { 0x0 };
            var name = $"{Guid.NewGuid()}.bin";
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");

            await bucket.Upload(data, name);

            DownloadProgressChangedEventHandler onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            var downloadPath = Path.Combine(basePath, name);
            var result = await bucket.Download(name, onProgress);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            Assert.IsTrue(data.SequenceEqual(result));

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Rename")]
        public async Task Move()
        {
            var name = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
            await bucket.Move(name, "new-file.bin");
            var items = await bucket.List();

            Assert.IsNotNull(items.Find((f) => f.Name == "new-file.bin"));
            Assert.IsNull(items.Find((f) => f.Name == name));
        }

        [TestMethod("File: Get Public Link")]
        public async Task GetPublicLink()
        {
            var name = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
            var url = bucket.GetPublicUrl(name);
            await bucket.Remove(new List<string> { name });

            Assert.IsNotNull(url);
        }

        [TestMethod("File: Get Signed Link")]
        public async Task GetSignedLink()
        {
            var name = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name);

            var url = bucket.CreateSignedUrl(name, 3600);
            Assert.IsNotNull(url);

            await bucket.Remove(new List<string> { name });
        }
    }
}

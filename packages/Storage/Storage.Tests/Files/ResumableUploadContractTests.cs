using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Storage.Tests.Files
{
    /// <summary>
    /// Contract tests for the resumable (TUS) upload flow driven through BirdMessenger, pinned
    /// hermetically so the interrupt-then-resume behaviour is deterministic: an upload with no cached
    /// session creates one and patches it, whereas an upload whose session URL is already cached resumes
    /// by patching that URL without creating a new session. This is the deterministic home for the logic
    /// the removed live 200 MB cancellation E2E tried — and, on CI, failed — to verify: cancelling a real
    /// upload mid-flight then resuming races the server-side committed offset, which cannot be made
    /// deterministic against a live server.
    /// </summary>
    [TestClass]
    [TestCategory("Contract")]
    public class ResumableUploadContractTests
    {
        private const string Bucket = "bucket";
        private const string FileName = "a.bin";
        // UploadOrContinue builds the cache key as "{bucket}/{object}/{contentType}"; a null FileOptions
        // defaults the content type, so this is the key a resume looks up.
        private const string CacheKey = "bucket/a.bin/text/plain;charset=UTF-8";
        private const string ResumablePath = "/storage/v1/upload/resumable";

        private static readonly byte[] Payload = { 0x1, 0x2, 0x3 };

        private WireMockServer server = null!;
        private Client client = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            UploadMemoryCache.Clear();
            this.server = WireMockServer.Start();
            this.client = new Client($"{this.server.Url}/storage/v1", new Dictionary<string, string> { { "Authorization", "Bearer test-key" } });
            this.StubTusEndpoints();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.server.Stop();
            UploadMemoryCache.Clear();
        }

        [TestMethod]
        public async Task UploadOrResume_ShouldCreateThenPatch_GivenNoCachedSession()
        {
            await this.client.From(Bucket).UploadOrResume(Payload, FileName, new FileOptions());
            using (new AssertionScope())
            {
                this.Paths("POST").Should().Contain(ResumablePath, "a first upload must create a TUS session");
                this.Paths("PATCH").Should().NotBeEmpty("the created session must then be patched with the bytes");
            }
        }

        [TestMethod]
        public async Task UploadOrResume_ShouldResumeCachedSessionWithoutCreating()
        {
            var cachedSession = $"{ResumablePath}/cached-session";
            UploadMemoryCache.Set(CacheKey, $"{this.server.Url}{cachedSession}");
            await this.client.From(Bucket).UploadOrResume(Payload, FileName, new FileOptions());
            using (new AssertionScope())
            {
                this.Paths("POST").Should().BeEmpty("a cached session must be resumed, never re-created");
                this.Paths("PATCH").Should().Contain(cachedSession, "the resume must patch the cached session URL");
            }
        }

        private void StubTusEndpoints()
        {
            var length = Payload.Length.ToString(CultureInfo.InvariantCulture);
            this.server.Given(Request.Create().UsingPost())
                .RespondWith(Response.Create().WithStatusCode(201)
                    .WithHeader("Location", $"{this.server.Url}{ResumablePath}/new-session")
                    .WithHeader("Tus-Resumable", "1.0.0")
                    .WithHeader("Upload-Offset", "0"));
            this.server.Given(Request.Create().UsingHead())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader("Tus-Resumable", "1.0.0")
                    .WithHeader("Upload-Offset", "0")
                    .WithHeader("Upload-Length", length));
            this.server.Given(Request.Create().UsingPatch())
                .RespondWith(Response.Create().WithStatusCode(204)
                    .WithHeader("Tus-Resumable", "1.0.0")
                    .WithHeader("Upload-Offset", length));
        }

        private IEnumerable<string> Paths(string method) =>
            this.server.LogEntries
                .Where(entry => entry.RequestMessage!.Method == method)
                .Select(entry => entry.RequestMessage!.Path);
    }
}

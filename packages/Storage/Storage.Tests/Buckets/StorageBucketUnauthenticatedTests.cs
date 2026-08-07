using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Storage.Tests;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace Storage.Tests.Buckets;

/// <summary>
/// End-to-end tests for bucket operations under the unauthenticated scenario — a request whose bearer
/// token is missing, malformed, or foreign (valid shape, wrong secret) — against a running local
/// Supabase stack. Each is rejected with a <see cref="SupabaseStorageException"/> carrying
/// <see cref="FailureHint.Reason.NotAuthorized"/>. This complements the service-role scenario
/// (<see cref="StorageBucketTests"/>) and the anon-key scenario (<see cref="StorageBucketAnonTests"/>).
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StorageBucketUnauthenticatedTests
{
    [TestMethod]
    public async Task CreateBucket_ShouldThrowNotAuthorized_GivenNoAuthorizationHeader()
    {
        var client = new Client(Helpers.StorageUrl);
        var act = () => client.CreateBucket("expected-to-fail");
        (await act.Should().ThrowAsync<SupabaseStorageException>())
            .Which.Reason.Should().Be(FailureHint.Reason.NotAuthorized);
    }

    [TestMethod]
    public async Task CreateBucket_ShouldThrowNotAuthorized_GivenGarbageToken()
    {
        var client = new Client(Helpers.StorageUrl, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer GarbageKey"
        });
        var act = () => client.CreateBucket("expected-to-fail");
        (await act.Should().ThrowAsync<SupabaseStorageException>())
            .Which.Reason.Should().Be(FailureHint.Reason.NotAuthorized);
    }

    [TestMethod]
    public async Task CreateBucket_ShouldThrowNotAuthorized_GivenForeignToken()
    {
        var client = new Client(Helpers.StorageUrl, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
        });
        var act = () => client.CreateBucket("expected-to-fail");
        (await act.Should().ThrowAsync<SupabaseStorageException>())
            .Which.Reason.Should().Be(FailureHint.Reason.NotAuthorized);
    }
}

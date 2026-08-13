using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace Storage.Tests.Buckets;

/// <summary>
/// End-to-end tests for bucket administration (list, get, create, update, empty, delete) against a
/// running local Supabase stack (<c>supabase start</c>), driving each operation with the service-role
/// client and asserting the observable outcome on the live storage service. Every bucket is provisioned
/// through the fixture so it is torn down whatever the outcome — see <see cref="StorageE2EFixture"/>.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StorageBucketTests : StorageE2EFixture
{
    [TestMethod]
    public async Task ListBuckets_ShouldReturnBucketsAndDistinguishFoldersFromFiles()
    {
        var id = await this.NewBucket();
        await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, "child/test.bin");
        var buckets = await this.Storage.ListBuckets();
        var parentFileList = await this.Storage.From(id).List();
        var childFileList = await this.Storage.From(id).List("child");
        using (new AssertionScope())
        {
            buckets!.Select(bucket => bucket.Id).Should().Contain(id);
            parentFileList!.First().IsFolder.Should().BeTrue();
            childFileList!.First().IsFolder.Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task GetBucket_ShouldResolveTheBucketOrNullWhenMissing()
    {
        var id = await this.NewBucket();
        using (new AssertionScope())
        {
            (await this.Storage.GetBucket(id)).Should().NotBeNull();
            (await this.Storage.GetBucket("I don't exist")).Should().BeNull();
        }
    }

    [TestMethod]
    public async Task CreateBucket_ShouldReturnTheIdAndDefaultToPrivate()
    {
        var id = Guid.NewGuid().ToString();
        var insertId = this.Track(await this.Storage.CreateBucket(id));
        var bucket = await this.Storage.GetBucket(id);
        using (new AssertionScope())
        {
            insertId.Should().Be(id);
            bucket!.Public.Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task CreateBucket_ShouldMakePublicAndRejectDuplicate()
    {
        var id = await this.NewBucket(new BucketUpsertOptions { Public = true });
        var bucket = await this.Storage.GetBucket(id);
        var act = () => this.Storage.CreateBucket(id);
        using (new AssertionScope())
        {
            bucket!.Public.Should().BeTrue();
            (await act.Should().ThrowAsync<SupabaseStorageException>())
                .Which.Reason.Should().Be(FailureHint.Reason.AlreadyExists);
        }
    }

    [TestMethod]
    public async Task UpdateBucket_ShouldFlipVisibilityToPublic()
    {
        var id = await this.NewBucket();
        (await this.Storage.GetBucket(id))!.Public.Should().BeFalse();
        await this.Storage.UpdateBucket(id, new BucketUpsertOptions { Public = true });
        (await this.Storage.GetBucket(id))!.Public.Should().BeTrue();
    }

    [TestMethod]
    public async Task EmptyBucket_ShouldRemoveAllObjects()
    {
        var id = await this.NewBucket();
        for (var i = 0; i < 5; i++)
            await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        (await this.Storage.From(id).List()).Should().NotBeEmpty();
        await this.Storage.EmptyBucket(id);
        (await this.Storage.From(id).List()).Should().BeEmpty();
    }

    [TestMethod]
    public async Task DeleteBucket_ShouldThrow_GivenNonEmptyBucket()
    {
        var id = await this.NewBucket();
        for (var i = 0; i < 5; i++)
            await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        var act = () => this.Storage.DeleteBucket(id);
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task DeleteBucket_ShouldRemoveTheBucket_GivenEmptied()
    {
        var id = await this.NewBucket();
        for (var i = 0; i < 5; i++)
            await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        await this.Storage.EmptyBucket(id);
        await this.Storage.DeleteBucket(id);
        (await this.Storage.GetBucket(id)).Should().BeNull();
    }
}

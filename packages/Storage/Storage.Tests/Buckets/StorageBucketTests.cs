using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Storage.Tests;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace Storage.Tests.Buckets;

/// <summary>
/// End-to-end tests for bucket administration (list, get, create, update, empty, delete) against a
/// running local Supabase stack (<c>supabase start</c>), driving each operation with the service-role
/// client and asserting the observable outcome on the live storage service.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StorageBucketTests
{
    private Client Storage => Helpers.GetServiceClient();

    [TestMethod]
    public async Task ListBuckets_ShouldReturnBucketsAndDistinguishFoldersFromFiles()
    {
        var buckets = await this.Storage.ListBuckets();
        buckets.Should().NotBeNull().And.NotBeEmpty();
        if (await this.Storage.GetBucket("parent") != null)
        {
            await this.Storage.From("parent").Remove("child/test.bin");
            await this.Storage.DeleteBucket("parent");
        }
        var newParentBucket = await this.Storage.CreateBucket("parent");
        newParentBucket.Should().NotBeNull();
        await this.Storage.From(newParentBucket).Upload(new byte[] { 0x0, 0x0, 0x0 }, "child/test.bin");
        var parentFileList = await this.Storage.From(newParentBucket).List();
        var childFileList = await this.Storage.From(newParentBucket).List("child");
        using (new AssertionScope())
        {
            parentFileList!.First().IsFolder.Should().BeTrue();
            childFileList!.First().IsFolder.Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task GetBucket_ShouldResolveTheBucketOrNullWhenMissing()
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id);
        using (new AssertionScope())
        {
            (await this.Storage.GetBucket(id)).Should().NotBeNull();
            (await this.Storage.GetBucket("I don't exist")).Should().BeNull();
        }
        await this.Storage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task CreateBucket_ShouldReturnTheIdAndDefaultToPrivate()
    {
        var id = Guid.NewGuid().ToString();
        var insertId = await this.Storage.CreateBucket(id);
        var bucket = await this.Storage.GetBucket(id);
        using (new AssertionScope())
        {
            insertId.Should().Be(id);
            bucket!.Public.Should().BeFalse();
        }
        await this.Storage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task CreateBucket_ShouldMakePublicAndRejectDuplicate()
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id, new BucketUpsertOptions { Public = true });
        var bucket = await this.Storage.GetBucket(id);
        var act = () => this.Storage.CreateBucket(id);
        using (new AssertionScope())
        {
            bucket!.Public.Should().BeTrue();
            (await act.Should().ThrowAsync<SupabaseStorageException>())
                .Which.Reason.Should().Be(FailureHint.Reason.AlreadyExists);
        }
        await this.Storage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task UpdateBucket_ShouldFlipVisibilityToPublic()
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id);
        (await this.Storage.GetBucket(id))!.Public.Should().BeFalse();
        await this.Storage.UpdateBucket(id, new BucketUpsertOptions { Public = true });
        (await this.Storage.GetBucket(id))!.Public.Should().BeTrue();
        await this.Storage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task EmptyBucket_ShouldRemoveAllObjects()
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id);
        for (var i = 0; i < 5; i++)
            await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        (await this.Storage.From(id).List()).Should().NotBeEmpty();
        await this.Storage.EmptyBucket(id);
        (await this.Storage.From(id).List()).Should().BeEmpty();
        await this.Storage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task DeleteBucket_ShouldThrow_GivenNonEmptyBucket()
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id);
        for (var i = 0; i < 5; i++)
            await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        var act = () => this.Storage.DeleteBucket(id);
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task DeleteBucket_ShouldRemoveTheBucket_GivenEmptied()
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id);
        for (var i = 0; i < 5; i++)
            await this.Storage.From(id).Upload(new byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        await this.Storage.EmptyBucket(id);
        await this.Storage.DeleteBucket(id);
        (await this.Storage.GetBucket(id)).Should().BeNull();
    }
}

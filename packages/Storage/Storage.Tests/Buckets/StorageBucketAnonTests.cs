using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Storage.Tests;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace Storage.Tests.Buckets;

/// <summary>
/// End-to-end tests asserting the anon (public-key) client is denied bucket administration against a
/// running local Supabase stack: listing yields nothing, getting an existing bucket resolves to null,
/// and create/update/empty/delete all surface a <see cref="SupabaseStorageException"/>.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StorageBucketAnonTests
{
    private Client AdminStorage => Helpers.GetServiceClient();
    private Client Storage => Helpers.GetPublicClient();

    [TestMethod]
    public async Task ListBuckets_ShouldBeEmptyAndDenyUpload()
    {
        (await this.Storage.ListBuckets()).Should().BeEmpty();
        var act = async () =>
        {
            var newParentBucket = "parent";
            if (await this.AdminStorage.GetBucket("parent") == null)
                newParentBucket = await this.AdminStorage.CreateBucket("parent");
            await this.Storage.From(newParentBucket).Upload(new byte[] { 0x0, 0x0, 0x0 }, "child/test.bin");
        };
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task GetBucket_ShouldReturnNull_GivenNoPermission()
    {
        var id = Guid.NewGuid().ToString();
        await this.AdminStorage.CreateBucket(id);
        (await this.Storage.GetBucket(id)).Should().BeNull();
        await this.AdminStorage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task CreateBucket_ShouldThrow_GivenNoPermission()
    {
        var act = () => this.Storage.CreateBucket("parent");
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task UpdateBucket_ShouldThrow_GivenNoPermission()
    {
        var id = Guid.NewGuid().ToString();
        await this.AdminStorage.CreateBucket(id);
        var act = () => this.Storage.UpdateBucket(id, new BucketUpsertOptions { Public = true });
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.AdminStorage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task EmptyBucket_ShouldThrow_GivenNoPermission()
    {
        var id = Guid.NewGuid().ToString();
        await this.AdminStorage.CreateBucket(id);
        var act = () => this.Storage.EmptyBucket(id);
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.AdminStorage.DeleteBucket(id);
    }

    [TestMethod]
    public async Task DeleteBucket_ShouldThrow_GivenNoPermission()
    {
        var id = Guid.NewGuid().ToString();
        await this.AdminStorage.CreateBucket(id);
        var act = () => this.Storage.DeleteBucket(id);
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.AdminStorage.DeleteBucket(id);
    }
}

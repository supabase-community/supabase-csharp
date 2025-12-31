using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace StorageTests;

[TestClass]
public class StorageBucketAnonTests
{
    private Client AdminStorage => Helpers.GetServiceClient();
    private Client Storage => Helpers.GetPublicClient();

    [TestMethod("Bucket: Returns empty when attempting to List")]
    public async Task List()
    {
        var buckets = await Storage.ListBuckets();

        Assert.IsNotNull(buckets);
        Assert.IsTrue(buckets.Count == 0);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            String newParentBucket = "parent";

            if (await AdminStorage.GetBucket("parent") == null)
                newParentBucket = await AdminStorage.CreateBucket("parent");

            await Storage.From(newParentBucket).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"child/test.bin");
        });
    }

    [TestMethod("Bucket: Returns null when attempting to Get")]
    public async Task Get()
    {
        var id = Guid.NewGuid().ToString();
        await AdminStorage.CreateBucket(id);

        var result = await Storage.GetBucket(id);
        Assert.IsNull(result);

        await AdminStorage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Throws when attempting to Create")]
    public async Task CreatePublic()
    {
        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await Storage.CreateBucket("parent");
        });
    }

    [TestMethod("Bucket: Throws when attempting to Update")]
    public async Task Update()
    {
        var id = Guid.NewGuid().ToString();
        await AdminStorage.CreateBucket(id);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await Storage.UpdateBucket(id, new BucketUpsertOptions { Public = true });
        });

        await AdminStorage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Throws when attempting to Empty")]
    public async Task Empty()
    {
        var id = Guid.NewGuid().ToString();
        await AdminStorage.CreateBucket(id);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await Storage.EmptyBucket(id);
        });
        
        await AdminStorage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Throws when attempting to Delete")]
    public async Task Delete()
    {
        var id = Guid.NewGuid().ToString();
        await AdminStorage.CreateBucket(id);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await Storage.DeleteBucket(id);
        });
        
        await AdminStorage.DeleteBucket(id);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace StorageTests;

[TestClass]
public class StorageBucketTests
{
    Client Storage => Helpers.GetServiceClient();

    [TestMethod("Bucket: List")]
    public async Task List()
    {
        var buckets = await Storage.ListBuckets();

        Assert.IsNotNull(buckets);
        Assert.IsTrue(buckets.Count > 0);
        Assert.IsInstanceOfType(buckets, typeof(List<Bucket>));

        if (await Storage.GetBucket("parent") != null)
        {
            await Storage.From("parent").Remove("child/test.bin");
            await Storage.DeleteBucket("parent");
        }

        var newParentBucket = await Storage.CreateBucket("parent");
        Assert.IsNotNull(newParentBucket);
        await Storage.From(newParentBucket).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"child/test.bin");
        
        var parentFileList = await Storage.From(newParentBucket).List();
        Assert.IsNotNull(parentFileList);
        Assert.IsTrue(parentFileList.First().IsFolder);

        var childFileList = await Storage.From(newParentBucket).List("child");
        Assert.IsNotNull(childFileList);
        Assert.IsFalse(childFileList.First().IsFolder);
    }

    [TestMethod("Bucket: Get")]
    public async Task Get()
    {
        var id = Guid.NewGuid().ToString();
        await Storage.CreateBucket(id);
        var bucket = await Storage.GetBucket(id);

        Assert.IsInstanceOfType(bucket, typeof(Bucket));

        var nonExisting = await Storage.GetBucket("I don't exist");
        Assert.IsNull(nonExisting);

        await Storage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Create, Private")]
    public async Task CreatePrivate()
    {
        var id = Guid.NewGuid().ToString();
        var insertId = await Storage.CreateBucket(id);

        Assert.AreEqual(id, insertId);

        var bucket = await Storage.GetBucket(id);

        Assert.IsNotNull(bucket);
        Assert.IsFalse(bucket.Public);

        await Storage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Create, Public")]
    public async Task CreatePublic()
    {
        var id = Guid.NewGuid().ToString();
        await Storage.CreateBucket(id, new BucketUpsertOptions { Public = true });

        var bucket = await Storage.GetBucket(id);

        Assert.IsNotNull(bucket);
        Assert.IsTrue(bucket.Public);

        var ex = await Assert.ThrowsExceptionAsync<SupabaseStorageException>(() => Storage.CreateBucket(id));
        Assert.IsTrue(ex.Reason == FailureHint.Reason.AlreadyExists);
        
        await Storage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Update")]
    public async Task Update()
    {
        var id = Guid.NewGuid().ToString();
        await Storage.CreateBucket(id);

        var privateBucket = await Storage.GetBucket(id);
        Assert.IsNotNull(privateBucket);
        Assert.IsFalse(privateBucket.Public);

        await Storage.UpdateBucket(id, new BucketUpsertOptions { Public = true });

        var nowPublicBucket = await Storage.GetBucket(id);
        Assert.IsNotNull(nowPublicBucket);
        Assert.IsTrue(nowPublicBucket.Public);

        await Storage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Empty")]
    public async Task Empty()
    {
        var id = Guid.NewGuid().ToString();
        await Storage.CreateBucket(id);

        for (var i = 0; i < 5; i++)
        {
            await Storage.From(id).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        }

        var initialList = await Storage.From(id).List();

        Assert.IsNotNull(initialList);
        Assert.IsTrue(initialList.Count > 0);

        await Storage.EmptyBucket(id);

        var listAfterEmpty = await Storage.From(id).List();

        Assert.IsNotNull(listAfterEmpty);
        Assert.IsTrue(listAfterEmpty.Count == 0);

        await Storage.DeleteBucket(id);
    }

    [TestMethod("Bucket: Delete, Throws Error if Not Empty")]
    public async Task DeleteThrows()
    {
        var id = Guid.NewGuid().ToString();
        await Storage.CreateBucket(id);

        for (var i = 0; i < 5; i++)
        {
            await Storage.From(id).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        }

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await Storage.DeleteBucket(id);
        });
    }

    [TestMethod("Bucket: Delete")]
    public async Task Delete()
    {
        var id = Guid.NewGuid().ToString();
        await Storage.CreateBucket(id);

        for (var i = 0; i < 5; i++)
        {
            await Storage.From(id).Upload(new Byte[] { 0x0, 0x0, 0x0 }, $"test-{i}.bin");
        }

        await Storage.EmptyBucket(id);
        await Storage.DeleteBucket(id);

        Assert.IsNull(await Storage.GetBucket(id));
    }
}
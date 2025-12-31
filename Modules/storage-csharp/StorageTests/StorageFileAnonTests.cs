using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;
using Supabase.Storage.Interfaces;

namespace StorageTests;

[TestClass]
public class StorageFileAnonTests
{
    Client AdminStorage => Helpers.GetServiceClient();
    private Client Storage => Helpers.GetPublicClient();

    private string _bucketId = string.Empty;
    private IStorageFileApi<FileObject> _adminBucket = null!;
    private IStorageFileApi<FileObject> _bucket = null!;

    [TestInitialize]
    public async Task InitializeTest()
    {
        _bucketId = Guid.NewGuid().ToString();

        if (_bucket == null && await Storage.GetBucket(_bucketId) == null)
        {
            await AdminStorage.CreateBucket(_bucketId, new BucketUpsertOptions { Public = false });
        }

        _adminBucket = AdminStorage.From(_bucketId);
        _bucket = Storage.From(_bucketId);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        var files = await _adminBucket.List();

        Assert.IsNotNull(files);

        foreach (var file in files)
        {
            if (file.Name is not null)
                await _adminBucket.Remove(new List<string> { file.Name });
        }

        await AdminStorage.DeleteBucket(_bucketId);
    }

    [TestMethod("File: Throws attempting to Upload File")]
    public async Task UploadFile()
    {
        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("file:", "");

        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.Upload(imagePath, name);
        });
    }

    [TestMethod("File: Throws attempting to Upload Arbitrary Byte Array")]
    public async Task UploadArbitraryByteArray()
    {
        var name = $"{Guid.NewGuid()}.bin";

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.Upload(new Byte[] { 0x0, 0x0, 0x0 }, name);
        });
    }

    [TestMethod("File: Throws attempting to Download")]
    public async Task DownloadFile()
    {
        var tsc = new TaskCompletionSource<bool>();

        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("file:", "");
        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        await _adminBucket.Upload(imagePath, name);

        var downloadPath = Path.Combine(basePath, name);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.Download(name, downloadPath, null);
        });

        await _adminBucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Throws attempting to Download Bytes")]
    public async Task DownloadBytes()
    {
        var data = new Byte[] { 0x0 };
        var name = $"{Guid.NewGuid()}.bin";

        await _adminBucket.Upload(data, name);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.Download(name, null);
        });

        await _adminBucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Throws attempting to Rename")]
    public async Task Move()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.Move(name, "new-file.bin");
        });
    }

    [TestMethod("File: Throws attempting to Copy")]
    public async Task Copy()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.Copy(name, "new-file.bin");
        });
    }
    
    [TestMethod("File: Get Public Link")]
    public async Task GetPublicLink()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        var url = _bucket.GetPublicUrl(name);
        await _adminBucket.Remove(new List<string> { name });

        Assert.IsNotNull(url);
    }

    [TestMethod("File: Throws attempting to Get Signed Link")]
    public async Task GetSignedLink()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            var url = await _bucket.CreateSignedUrl(name, 3600);
        });

        await _adminBucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Throws attempting to Get Multiple Signed Links")]
    public async Task GetMultipleSignedLinks()
    {
        var name1 = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name1);

        var name2 = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name2);

        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.CreateSignedUrls(new List<string> { name1, name2 }, 3600);
        });

        await _adminBucket.Remove(new List<string> { name1 });
    }

    [TestMethod("File: Throws attempting to Create Signed Upload Url")]
    public async Task CanCreateSignedUploadUrl()
    {
        var name1 = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name1);

        var name2 = $"{Guid.NewGuid()}.bin";
        await _adminBucket.Upload(new Byte[] { 0x0, 0x1 }, name2);
        
        await Assert.ThrowsExceptionAsync<SupabaseStorageException>(async () =>
        {
            await _bucket.CreateSignedUrls(new List<string> { name1, name2 }, 3600);
        });
    }
}
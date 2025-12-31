using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Interfaces;
using FileOptions = Supabase.Storage.FileOptions;

namespace StorageTests;

[TestClass]
public class StorageFileTests
{
    Client Storage => Helpers.GetServiceClient();

    private string _bucketId = string.Empty;
    private IStorageFileApi<FileObject> _bucket = null!;

    [TestInitialize]
    public async Task InitializeTest()
    {
        _bucketId = Guid.NewGuid().ToString();

        var exists = await Storage.GetBucket(_bucketId);
        if (exists == null)
        {
            await Storage.CreateBucket(_bucketId, new BucketUpsertOptions { Public = true });
        }

        _bucket = Storage.From(_bucketId);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        var files = await _bucket.List();

        Assert.IsNotNull(files);

        foreach (var file in files)
        {
            if (file.Name is not null)
                await _bucket.Remove(new List<string> { file.Name });
        }

        await Storage.DeleteBucket(_bucketId);
    }

    [TestMethod("File: Upload File")]
    public async Task UploadFile()
    {
        var didTriggerProgress = new TaskCompletionSource<bool>();

        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?.Replace("file:", "");

        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        await _bucket.Upload(
            imagePath,
            name,
            null,
            (_, _) =>
            {
                didTriggerProgress.TrySetResult(true);
            }
        );

        var list = await _bucket.List();

        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing);

        var sentProgressEvent = await didTriggerProgress.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Resume Upload File")]
    public async Task UploadResumableFile()
    {
        var didTriggerProgress = new TaskCompletionSource<bool>();
        var name = $"{Guid.NewGuid()}.png";
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        var data = new byte[2 * 1024 * 1024]; 
        var rng = new Random();
        rng.NextBytes(data);
        await File.WriteAllBytesAsync(tempFilePath, data);

        try
        {
            var metadata = new Dictionary<string, string>
            {
                ["custom"] = "metadata",
                ["local_file"] = "local_file",
            };

            var headers = new Dictionary<string, string> { ["x-version"] = "123" };

            var options = new FileOptions
            {
                Duplex = "duplex",
                Metadata = metadata,
                Headers = headers,
            };

            await _bucket.UploadOrResume(
                tempFilePath,
                name,
                options,
                (x, y) =>
                {
                    Console.WriteLine($"Progress {y}");
                    didTriggerProgress.TrySetResult(true);
                }
            );

            var list = await _bucket.List();

            Assert.IsNotNull(list);

            var existing = list.Find(item => item.Name == name);
            Assert.IsNotNull(existing);

            var sentProgressEvent = await didTriggerProgress.Task;
            Assert.IsTrue(sentProgressEvent);

            await _bucket.Remove([name]);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [TestMethod("File Resume Upload File as Byte")]
    public async Task UploadResumableByte()
    {
        var didTriggerProgress = new TaskCompletionSource<bool>();
        var data = new byte[1 * 1024 * 1024];
        var rng = new Random();
        rng.NextBytes(data);
        var name = $"{Guid.NewGuid()}.png";
        var metadata = new Dictionary<string, string>
        {
            ["custom"] = "metadata",
            ["local_file"] = "local_file",
        };

        var headers = new Dictionary<string, string> { ["x-version"] = "123" };

        var options = new FileOptions
        {
            Duplex = "duplex",
            Metadata = metadata,
            Headers = headers,
        };

        await _bucket.UploadOrResume(
            data,
            name,
            options,
            (x, y) =>
            {
                Console.WriteLine($"Progress {y}");
                didTriggerProgress.TrySetResult(true);
            }
        );

        var list = await _bucket.List();

        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing);

        var sentProgressEvent = await didTriggerProgress.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove([name]);
    }

    [TestMethod("File: Resume Upload as Byte override existing one")]
    public async Task UploadResumableByteDuplicate()
    {
        var didTriggerProgress = new TaskCompletionSource<bool>();
        var data = new byte[1 * 1024 * 1024];
        var rng = new Random();
        rng.NextBytes(data);
        var name = $"{Guid.NewGuid()}.png";
        var metadata = new Dictionary<string, string>
        {
            ["custom"] = "metadata",
            ["local_file"] = "local_file",
        };

        var options = new FileOptions
        {
            Duplex = "duplex",
            Metadata = metadata,
            Upsert = true,
        };

        await _bucket.UploadOrResume(
            data,
            name,
            options,
            (x, y) =>
            {
                Console.WriteLine($"Progress {y}");
                didTriggerProgress.TrySetResult(true);
            }
        );

        await _bucket.UploadOrResume(
            data,
            name,
            options,
            (x, y) =>
            {
                Console.WriteLine($"Progress {y}");
                didTriggerProgress.TrySetResult(true);
            }
        );

        var list = await _bucket.List();

        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing);

        var sentProgressEvent = await didTriggerProgress.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove([name]);
    }

    [TestMethod("File: Resume Upload with interruption and resume using CancellationToken")]
    public async Task UploadOrResumeByteWithInterruptionAndResume()
    {
        var firstUploadProgressTriggered = new TaskCompletionSource<bool>();
        var resumeUploadProgressTriggered = new TaskCompletionSource<bool>();

        var data = new byte[200 * 1024 * 1024];
        var rng = new Random();
        rng.NextBytes(data);
        var name = $"{Guid.NewGuid()}.bin";

        var metadata = new Dictionary<string, string>
        {
            ["custom"] = "metadata",
            ["local_file"] = "local_file",
        };

        var options = new FileOptions { Duplex = "duplex", Metadata = metadata };

        using var cts = new CancellationTokenSource();

        try
        {
            await _bucket.UploadOrResume(
                data,
                name,
                options,
                (_, progress) =>
                {
                    if (progress > 20)
                        cts.Cancel();
                    
                    Console.WriteLine($"First upload progress: {progress}");
                    firstUploadProgressTriggered.TrySetResult(true);
                },
                cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("First upload was cancelled as expected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"First upload failed with unexpected error: {ex.Message}");
            Assert.Fail($"First upload should have been cancelled, but failed with: {ex.Message}");
        }

        var firstProgressTriggered =
            await Task.WhenAny(
                firstUploadProgressTriggered.Task,
                Task.Delay(TimeSpan.FromSeconds(2))
            ) == firstUploadProgressTriggered.Task;

        Assert.IsTrue(
            firstProgressTriggered,
            "First upload progress event should have been triggered"
        );

        await _bucket.UploadOrResume(
            data,
            name,
            options,
            (_, progress) =>
            {
                Console.WriteLine($"Resume progress: {progress}");
                resumeUploadProgressTriggered.TrySetResult(true);
            }
        );

        var resumeProgressTriggered = await resumeUploadProgressTriggered.Task;
        Assert.IsTrue(resumeProgressTriggered, "Resume progress event should have been triggered");

        var list = await _bucket.List();
        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing, "File should exist in bucket after resumed upload");

        await _bucket.Remove([name]);
    }

    [TestMethod("File: Upload File With FileOptions")]
    public async Task UploadFileWithFileOptions()
    {
        var didTriggerProgress = new TaskCompletionSource<bool>();

        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?.Replace("file:", "");

        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        var metadata = new Dictionary<string, string>
        {
            ["custom"] = "metadata",
            ["local_file"] = "local_file",
        };

        var headers = new Dictionary<string, string> { ["x-version"] = "123" };

        var options = new FileOptions
        {
            Duplex = "duplex",
            Metadata = metadata,
            Headers = headers,
        };
        await _bucket.Upload(
            imagePath,
            name,
            options,
            (_, _) =>
            {
                didTriggerProgress.TrySetResult(true);
            }
        );

        var item = await _bucket.Info(name);

        Assert.IsNotNull(item);
        Assert.IsNotNull(item.Metadata);
        Assert.AreEqual(metadata["custom"], item.Metadata["custom"]);
        Assert.AreEqual(metadata["local_file"], item.Metadata["local_file"]);

        var sentProgressEvent = await didTriggerProgress.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove([name]);
    }

    [TestMethod("File: Upload Arbitrary Byte Array")]
    public async Task UploadArbitraryByteArray()
    {
        var tsc = new TaskCompletionSource<bool>();

        var name = $"{Guid.NewGuid()}.bin";

        await _bucket.Upload(
            new Byte[] { 0x0, 0x0, 0x0 },
            name,
            null,
            (_, _) => tsc.TrySetResult(true)
        );

        var list = await _bucket.List();
        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing);

        var sentProgressEvent = await tsc.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove(new List<string> { name });
    }
    
    [TestMethod("File: Cancel Upload Arbitrary Byte Array")]
    public async Task UploadArbitraryByteArrayCanceled()
    {
        var tsc = new TaskCompletionSource<bool>();
        using var ctk = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

        var data = new byte[20 * 1024 * 1024];
        var rng = new Random();
        rng.NextBytes(data);
        var name = $"{Guid.NewGuid()}.bin";

        var action = async () =>
        {
            await _bucket.Upload(data, name, null, (_, _) => tsc.TrySetResult(true), true, ctk.Token);
        };

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(action);

        var list = await _bucket.List();
        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNull(existing);

        await _bucket.Remove([name]);
    }

    [TestMethod("File: Download")]
    public async Task DownloadFile()
    {
        var tsc = new TaskCompletionSource<bool>();

        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?.Replace("file:", "");
        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        await _bucket.Upload(imagePath, name);

        var downloadPath = Path.Combine(basePath, name);
        await _bucket.Download(name, downloadPath, (_, _) => tsc.TrySetResult(true));

        var sentProgressEvent = await tsc.Task;
        Assert.IsTrue(sentProgressEvent);

        Assert.IsTrue(File.Exists(downloadPath));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Download Bytes")]
    public async Task DownloadBytes()
    {
        var tsc = new TaskCompletionSource<bool>();

        var data = new Byte[] { 0x0 };
        var name = $"{Guid.NewGuid()}.bin";

        await _bucket.Upload(data, name);

        var result = await _bucket.Download(name, (_, _) => tsc.TrySetResult(true));

        var sentProgressEvent = await tsc.Task;

        Assert.IsTrue(sentProgressEvent);
        Assert.IsTrue(data.SequenceEqual(result));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Rename")]
    public async Task Move()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        await _bucket.Move(name, "new-file.bin");
        var items = await _bucket.List();

        Assert.IsNotNull(items);

        Assert.IsNotNull(items.Find((f) => f.Name == "new-file.bin"));
        Assert.IsNull(items.Find((f) => f.Name == name));
    }

    [TestMethod("File: Copy")]
    public async Task Copy()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload([0x0, 0x1], name);
        await _bucket.Copy(name, "new-file.bin");
        var items = await _bucket.List();

        Assert.IsNotNull(items);

        Assert.IsNotNull(items.Find((f) => f.Name == "new-file.bin"));
        Assert.IsNotNull(items.Find((f) => f.Name == name));
    }

    [TestMethod("File: Copy to another Bucket")]
    public async Task CopyToAnotherBucket()
    {
        await Storage.CreateBucket("copyfile", new BucketUpsertOptions { Public = true });
        var localBucket = Storage.From("copyfile");

        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload([0x0, 0x1], name);

        await _bucket.Copy(
            name,
            "new-file.bin",
            new DestinationOptions { DestinationBucket = "copyfile" }
        );
        var items = await _bucket.List();
        var copied = await localBucket.List();

        Assert.IsNotNull(items);
        Assert.IsNotNull(copied);

        Assert.IsNotNull(copied.Find((f) => f.Name == "new-file.bin"));
        Assert.IsNotNull(items.Find((f) => f.Name == name));

        foreach (var file in copied)
        {
            if (file.Name is not null)
                await localBucket.Remove([file.Name]);
        }

        await Storage.DeleteBucket("copyfile");
    }

    [TestMethod("File: Get Public Link")]
    public async Task GetPublicLink()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        var url = _bucket.GetPublicUrl(name);
        await _bucket.Remove(new List<string> { name });

        Assert.IsNotNull(url);
    }

    [TestMethod("File: Get Public Link with download options")]
    public async Task GetPublicLinkWithDownloadOptions()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        var url = _bucket.GetPublicUrl(
            name,
            null,
            new DownloadOptions { FileName = "custom-file.png" }
        );
        await _bucket.Remove(new List<string> { name });

        Assert.IsNotNull(url);
        StringAssert.Contains(url, "download=custom-file.png");
    }

    [TestMethod("File: Get Public Link with download and transform options")]
    public async Task GetPublicLinkWithDownloadAndTransformOptions()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        var url = _bucket.GetPublicUrl(
            name,
            new TransformOptions { Height = 100, Width = 100 },
            DownloadOptions.UseOriginalFileName
        );
        await _bucket.Remove(new List<string> { name });

        Assert.IsNotNull(url);
        StringAssert.Contains(url, "download=true");
    }

    [TestMethod("File: Get Signed Link")]
    public async Task GetSignedLink()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        var url = await _bucket.CreateSignedUrl(name, 3600);
        Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Get Signed Link with transform options")]
    public async Task GetSignedLinkWithTransformOptions()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        var url = await _bucket.CreateSignedUrl(
            name,
            3600,
            new TransformOptions { Width = 100, Height = 100 }
        );
        Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Get Signed Link with download options")]
    public async Task GetSignedLinkWithDownloadOptions()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        var url = await _bucket.CreateSignedUrl(
            name,
            3600,
            null,
            new DownloadOptions { FileName = "custom-file.png" }
        );
        Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));
        StringAssert.Contains(url, "download=custom-file.png");

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Get Multiple Signed Links")]
    public async Task GetMultipleSignedLinks()
    {
        var name1 = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name1);

        var name2 = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name2);

        var urls = await _bucket.CreateSignedUrls(
            new List<string> { name1, name2 },
            3600,
            DownloadOptions.UseOriginalFileName
        );

        Assert.IsNotNull(urls);

        foreach (var response in urls)
        {
            Assert.IsTrue(Uri.IsWellFormedUriString($"{response.SignedUrl}", UriKind.Absolute));
            StringAssert.Contains(response.SignedUrl, "download=true");
        }

        await _bucket.Remove(new List<string> { name1 });
    }

    [TestMethod("File: Can Create Signed Upload Url")]
    public async Task CanCreateSignedUploadUrl()
    {
        var result = await _bucket.CreateUploadSignedUrl("test.png");
        Assert.IsTrue(Uri.IsWellFormedUriString(result.SignedUrl.ToString(), UriKind.Absolute));
    }
}


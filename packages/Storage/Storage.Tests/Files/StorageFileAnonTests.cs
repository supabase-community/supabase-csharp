using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;
using Supabase.Storage.Interfaces;

namespace Storage.Tests.Files;

/// <summary>
/// End-to-end tests asserting that the anon (public-key) client is denied file operations on a
/// private bucket against a running local Supabase stack: upload, download, move, copy and signing
/// all surface a <see cref="SupabaseStorageException"/>, while building a public URL stays allowed.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StorageFileAnonTests : StorageE2EFixture
{
    private IStorageFileApi<FileObject> adminBucket = null!;
    private IStorageFileApi<FileObject> bucket = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        var bucketId = await this.NewBucket(new BucketUpsertOptions { Public = false });
        this.adminBucket = this.Storage.From(bucketId);
        this.bucket = this.PublicStorage.From(bucketId);
    }

    [TestMethod]
    public async Task Upload_ShouldThrow_GivenFileSource()
    {
        var imagePath = Path.Combine(BasePath(), "Assets", "supabase-csharp.png");
        var act = () => this.bucket.Upload(imagePath, $"{Guid.NewGuid()}.png");
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task Upload_ShouldThrow_GivenByteArray()
    {
        var act = () => this.bucket.Upload(new byte[] { 0x0, 0x0, 0x0 }, $"{Guid.NewGuid()}.bin");
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task Download_ShouldThrow_GivenFileTarget()
    {
        var name = $"{Guid.NewGuid()}.png";
        var imagePath = Path.Combine(BasePath(), "Assets", "supabase-csharp.png");
        await this.adminBucket.Upload(imagePath, name);
        var act = () => this.bucket.Download(name, Path.Combine(BasePath(), name), (EventHandler<float>?) null);
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.adminBucket.Remove(new List<string> { name });
    }

    [TestMethod]
    public async Task Download_ShouldThrow_GivenByteTarget()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0 }, name);
        var act = () => this.bucket.Download(name, (EventHandler<float>?) null);
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.adminBucket.Remove(new List<string> { name });
    }

    [TestMethod]
    public async Task Move_ShouldThrow_GivenNoPermission()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name);
        var act = () => this.bucket.Move(name, "new-file.bin");
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task Copy_ShouldThrow_GivenNoPermission()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name);
        var act = () => this.bucket.Copy(name, "new-file.bin");
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    [TestMethod]
    public async Task GetPublicUrl_ShouldBeAllowed()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name);
        this.bucket.GetPublicUrl(name, null).Should().NotBeNull();
        await this.adminBucket.Remove(new List<string> { name });
    }

    [TestMethod]
    public async Task CreateSignedUrl_ShouldThrow_GivenNoPermission()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name);
        var act = () => this.bucket.CreateSignedUrl(name, 3600);
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.adminBucket.Remove(new List<string> { name });
    }

    [TestMethod]
    public async Task CreateSignedUrls_ShouldThrow_GivenNoPermission()
    {
        var name1 = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name1);
        var name2 = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name2);
        var act = () => this.bucket.CreateSignedUrls(new List<string> { name1, name2 }, 3600);
        await act.Should().ThrowAsync<SupabaseStorageException>();
        await this.adminBucket.Remove(new List<string> { name1 });
    }

    [TestMethod]
    public async Task CreateSignedUrls_ShouldThrow_GivenNoPermissionToSignForUpload()
    {
        var name1 = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name1);
        var name2 = $"{Guid.NewGuid()}.bin";
        await this.adminBucket.Upload(new byte[] { 0x0, 0x1 }, name2);
        var act = () => this.bucket.CreateSignedUrls(new List<string> { name1, name2 }, 3600);
        await act.Should().ThrowAsync<SupabaseStorageException>();
    }

    private static string BasePath() =>
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!.Replace("file:", "");
}

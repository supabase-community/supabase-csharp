using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace Storage.Tests.Files;

/// <summary>
/// Covers <see cref="UploadMemoryCache"/>, the store of resumable-upload URLs: storing and
/// retrieving by key, guarding blank keys/urls, overwrite semantics, removal, clearing, and that an
/// entry stops resolving once its time-to-live has elapsed.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class UploadMemoryCacheTests
{
    [TestInitialize]
    public void TestInitialize() => UploadMemoryCache.Clear();

    [TestCleanup]
    public void TestCleanup()
    {
        UploadMemoryCache.Clear();
        UploadMemoryCache.SetDefaultTtl(TimeSpan.FromMinutes(60));
    }

    [TestMethod]
    public void Set_ShouldStoreUrlRetrievableByKey()
    {
        UploadMemoryCache.Set("key", "https://upload/resumable");
        UploadMemoryCache.TryGet("key", out var url).Should().BeTrue();
        url.Should().Be("https://upload/resumable");
    }

    [TestMethod]
    public void TryGet_ShouldReturnFalse_GivenUnknownKey()
    {
        UploadMemoryCache.TryGet("missing", out var url).Should().BeFalse();
        url.Should().BeNull();
    }

    [TestMethod]
    public void TryGet_ShouldReturnFalse_GivenBlankKey() =>
        UploadMemoryCache.TryGet("  ", out _).Should().BeFalse();

    [TestMethod]
    public void Set_ShouldThrow_GivenBlankKey()
    {
        var act = () => UploadMemoryCache.Set(" ", "https://upload");
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Set_ShouldThrow_GivenBlankUrl()
    {
        var act = () => UploadMemoryCache.Set("key", " ");
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Set_ShouldOverwrite_GivenExistingKey()
    {
        UploadMemoryCache.Set("key", "https://first");
        UploadMemoryCache.Set("key", "https://second");
        UploadMemoryCache.TryGet("key", out var url);
        url.Should().Be("https://second");
    }

    [TestMethod]
    public void Remove_ShouldDropEntryAndReturnTrue_GivenExistingKey()
    {
        UploadMemoryCache.Set("key", "https://upload");
        UploadMemoryCache.Remove("key").Should().BeTrue();
        UploadMemoryCache.TryGet("key", out _).Should().BeFalse();
    }

    [TestMethod]
    public void Remove_ShouldReturnFalse_GivenUnknownKey() =>
        UploadMemoryCache.Remove("missing").Should().BeFalse();

    [TestMethod]
    public void Remove_ShouldReturnFalse_GivenBlankKey() =>
        UploadMemoryCache.Remove(" ").Should().BeFalse();

    [TestMethod]
    public void Clear_ShouldEmptyTheCache()
    {
        UploadMemoryCache.Set("a", "https://a");
        UploadMemoryCache.Set("b", "https://b");
        UploadMemoryCache.Clear();
        UploadMemoryCache.Count.Should().Be(0);
    }

    [TestMethod]
    public void Count_ShouldReflectStoredEntries()
    {
        UploadMemoryCache.Set("a", "https://a");
        UploadMemoryCache.Set("b", "https://b");
        UploadMemoryCache.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task TryGet_ShouldReturnFalse_GivenExpiredEntry()
    {
        UploadMemoryCache.Set("key", "https://upload", TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);
        UploadMemoryCache.TryGet("key", out _).Should().BeFalse();
    }
}

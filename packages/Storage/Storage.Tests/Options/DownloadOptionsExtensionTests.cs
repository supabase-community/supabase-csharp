using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Extensions;

namespace Storage.Tests.Options;

/// <summary>
/// Covers <see cref="DownloadOptionsExtension.ToQueryCollection"/>: a null file name emits no
/// download parameter, an empty name forces the original file name (<c>download=true</c>), and a
/// concrete name is passed through as the download attribute.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DownloadOptionsExtensionTests
{
    [TestMethod]
    public void ToQueryCollection_ShouldEmitNoDownloadParam_GivenNullFileName()
    {
        var query = new DownloadOptions().ToQueryCollection();
        query.AllKeys.Should().NotContain("download");
    }

    [TestMethod]
    public void ToQueryCollection_ShouldEmitDownloadTrue_GivenEmptyFileName()
    {
        var query = DownloadOptions.UseOriginalFileName.ToQueryCollection();
        query["download"].Should().Be("true");
    }

    [TestMethod]
    public void ToQueryCollection_ShouldEmitTheFileName_GivenAName()
    {
        var query = new DownloadOptions { FileName = "custom-file.png" }.ToQueryCollection();
        query["download"].Should().Be("custom-file.png");
    }

    [TestMethod]
    public void ToQueryCollection_ShouldEmitTheCacheNonce_GivenACacheNonce()
    {
        var query = new DownloadOptions { CacheNonce = "nonce-123" }.ToQueryCollection();
        query["cacheNonce"].Should().Be("nonce-123");
    }
}

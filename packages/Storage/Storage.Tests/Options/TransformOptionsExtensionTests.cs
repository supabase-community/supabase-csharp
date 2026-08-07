using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Extensions;

namespace Storage.Tests.Options;

/// <summary>
/// Covers <see cref="TransformOptionsExtension.ToQueryCollection"/>: the image-render query string
/// carries format, resize (mapped from the enum) and quality on every request, and width/height
/// only when the caller set them.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class TransformOptionsExtensionTests
{
    [TestMethod]
    public void ToQueryCollection_ShouldAlwaysEmitFormatResizeAndQuality()
    {
        var query = new TransformOptions().ToQueryCollection();
        using (new AssertionScope())
        {
            query["format"].Should().Be("origin");
            query["resize"].Should().Be("cover");
            query["quality"].Should().Be("80");
        }
    }

    [TestMethod]
    public void ToQueryCollection_ShouldOmitWidthAndHeight_GivenUnset()
    {
        var query = new TransformOptions().ToQueryCollection();
        using (new AssertionScope())
        {
            query.AllKeys.Should().NotContain("width");
            query.AllKeys.Should().NotContain("height");
        }
    }

    [TestMethod]
    public void ToQueryCollection_ShouldEmitWidthAndHeight_GivenSet()
    {
        var query = new TransformOptions { Width = 120, Height = 80 }.ToQueryCollection();
        using (new AssertionScope())
        {
            query["width"].Should().Be("120");
            query["height"].Should().Be("80");
        }
    }

    [TestMethod]
    public void ToQueryCollection_ShouldMapResizeContain()
    {
        var query = new TransformOptions { Resize = TransformOptions.ResizeType.Contain }.ToQueryCollection();
        query["resize"].Should().Be("contain");
    }

    [TestMethod]
    public void ToQueryCollection_ShouldMapResizeFill()
    {
        var query = new TransformOptions { Resize = TransformOptions.ResizeType.Fill }.ToQueryCollection();
        query["resize"].Should().Be("fill");
    }

    [TestMethod]
    public void ToQueryCollection_ShouldEmitTheOverriddenQuality()
    {
        var query = new TransformOptions { Quality = 55 }.ToQueryCollection();
        query["quality"].Should().Be("55");
    }
}

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Core.Attributes;

namespace Core.Tests.Attributes;

/// <summary>
/// Covers <see cref="MapToAttribute"/>: the mapping and optional formatter captured at construction,
/// with the formatter defaulting to null when omitted.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class MapToAttributeTests
{
    [TestMethod]
    public void Constructor_ShouldCaptureTheMapping() =>
        new MapToAttribute("refresh_token").Mapping.Should().Be("refresh_token");

    [TestMethod]
    public void Constructor_ShouldDefaultFormatterToNull() =>
        new MapToAttribute("refresh_token").Formatter.Should().BeNull();

    [TestMethod]
    public void Constructor_ShouldCaptureTheFormatter_GivenOne()
    {
        var attribute = new MapToAttribute("created_at", "O");
        using (new AssertionScope())
        {
            attribute.Mapping.Should().Be("created_at");
            attribute.Formatter.Should().Be("O");
        }
    }
}

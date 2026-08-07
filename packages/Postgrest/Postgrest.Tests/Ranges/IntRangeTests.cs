using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;

namespace Postgrest.Tests.Ranges;

/// <summary>
///     The value semantics postgrest relies on for <see cref="IntRange" />: two ranges over the same bounds
///     are equal (so filter/coercion round-trips can be asserted), and the bounds are exposed as their
///     integer values.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class IntRangeTests
{
    [TestMethod]
    public void Bounds_ShouldExposeTheConstructedStartAndEnd()
    {
        var range = new IntRange(20, 50);
        using (new AssertionScope())
        {
            range.Start.Value.Should().Be(20);
            range.End.Value.Should().Be(50);
        }
    }

    [TestMethod]
    public void Equals_ShouldBeTrue_GivenTheSameBounds()
    {
        new IntRange(1, 2).Equals(new IntRange(1, 2)).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_ShouldBeFalse_GivenDifferentBounds()
    {
        using (new AssertionScope())
        {
            new IntRange(1, 2).Equals(new IntRange(1, 3)).Should().BeFalse();
            new IntRange(1, 2).Equals(new IntRange(0, 2)).Should().BeFalse();
        }
    }

    [TestMethod]
    public void GetHashCode_ShouldMatch_GivenEqualRanges()
    {
        new IntRange(1, 2).GetHashCode().Should().Be(new IntRange(1, 2).GetHashCode());
    }
}

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace Storage.Tests.Options;

/// <summary>
/// Covers the default listing sort a new <see cref="SortBy"/> carries: ascending by the
/// <c>name</c> column unless a caller overrides either facet.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SortByTests
{
    [TestMethod]
    public void SortBy_ShouldDefaultToNameAscending()
    {
        var sortBy = new SortBy();
        using (new AssertionScope())
        {
            sortBy.Column.Should().Be("name");
            sortBy.Order.Should().Be("asc");
        }
    }

    [TestMethod]
    public void SortBy_ShouldKeepDefaultOrder_GivenOnlyColumnOverridden()
    {
        var sortBy = new SortBy { Column = "status" };
        using (new AssertionScope())
        {
            sortBy.Column.Should().Be("status");
            sortBy.Order.Should().Be("asc");
        }
    }

    [TestMethod]
    public void SortBy_ShouldKeepDefaultColumn_GivenOnlyOrderOverridden()
    {
        var sortBy = new SortBy { Order = "desc" };
        using (new AssertionScope())
        {
            sortBy.Column.Should().Be("name");
            sortBy.Order.Should().Be("desc");
        }
    }
}

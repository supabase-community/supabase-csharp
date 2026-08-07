using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace Storage.Tests.Options;

/// <summary>
/// Covers the defaults a new <see cref="SearchOptions"/> carries for a listing request: the paging
/// window, empty search filter, and a name-ascending <see cref="SortBy"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SearchOptionsTests
{
    [TestMethod]
    public void SearchOptions_ShouldDefaultToNameAscendingSort()
    {
        var options = new SearchOptions();
        using (new AssertionScope())
        {
            options.SortBy.Column.Should().Be("name");
            options.SortBy.Order.Should().Be("asc");
        }
    }

    [TestMethod]
    public void SearchOptions_ShouldDefaultTheSearchWindow()
    {
        var options = new SearchOptions();
        using (new AssertionScope())
        {
            options.Limit.Should().Be(100);
            options.Offset.Should().Be(0);
            options.Search.Should().BeEmpty();
        }
    }
}

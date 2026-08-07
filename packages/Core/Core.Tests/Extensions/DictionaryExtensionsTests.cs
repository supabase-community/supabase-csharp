using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Core.Extensions;

namespace Core.Tests.Extensions;

/// <summary>
/// Covers <see cref="DictionaryExtensions.MergeLeft{T,TK,TV}"/>: a new dictionary combining every
/// source, with later sources overwriting earlier keys, leaving the originals untouched.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DictionaryExtensionsTests
{
    [TestMethod]
    public void MergeLeft_ShouldCombineEntriesFromEverySource() =>
        new Dictionary<string, int> { ["x"] = 1 }
            .MergeLeft(new Dictionary<string, int> { ["y"] = 2 })
            .Should().Equal(new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 });

    [TestMethod]
    public void MergeLeft_ShouldPreferLaterSources_GivenOverlappingKeys() =>
        new Dictionary<string, int> { ["k"] = 1 }
            .MergeLeft(new Dictionary<string, int> { ["k"] = 2 })
            .Should().Contain("k", 2);

    [TestMethod]
    public void MergeLeft_ShouldLeaveTheSourceUnchanged()
    {
        var source = new Dictionary<string, int> { ["x"] = 1 };
        source.MergeLeft(new Dictionary<string, int> { ["y"] = 2 });
        source.Should().Equal(new Dictionary<string, int> { ["x"] = 1 });
    }
}

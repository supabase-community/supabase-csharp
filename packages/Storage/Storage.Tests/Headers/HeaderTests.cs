using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace Storage.Tests.Headers;

/// <summary>
/// Covers <see cref="Header"/>: header keys are normalised to lower-case, and adding a key that
/// already exists (in any casing) replaces the previous value rather than duplicating it.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class HeaderTests
{
    [TestMethod]
    public void Add_ShouldLowerCaseTheKey()
    {
        var header = new Header();
        header.Add("Content-Type", "application/json");
        header.Get().Should().ContainKey("content-type").WhoseValue.Should().Be("application/json");
    }

    [TestMethod]
    public void Add_ShouldStoreEveryEntry_GivenADictionary()
    {
        var header = new Header();
        header.Add(new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "X-Custom-Header", "Value" }
        });
        header.Get().Should().HaveCount(2)
            .And.Contain("content-type", "application/json")
            .And.Contain("x-custom-header", "Value");
    }

    [TestMethod]
    public void Add_ShouldReplaceExistingValue_GivenSameKey()
    {
        var header = new Header();
        header.Add("Content-Type", "application/json");
        header.Add("CONTENT-TYPE", "text/plain");
        header.Get().Should().ContainSingle().Which.Should().BeEquivalentTo(
            new KeyValuePair<string, string>("content-type", "text/plain"));
    }

    [TestMethod]
    public void Add_ShouldReplaceExistingValue_GivenSameKeyInDifferentCase()
    {
        var header = new Header();
        header.Add("X-Custom", "value1");
        header.Add("x-custom", "value2");
        header.Get().Should().ContainSingle().Which.Value.Should().Be("value2");
    }
}

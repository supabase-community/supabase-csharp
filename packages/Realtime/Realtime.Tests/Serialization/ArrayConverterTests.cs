using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Realtime;
using Supabase.Realtime.Converters;

namespace Realtime.Tests.Serialization;

/// <summary>
///     WALRUS delivers Postgres array columns as strings in either <c>{1,2,3}</c> (curly) or <c>[1,2,3]</c>
///     (bracket) form; <see cref="IntArrayConverter" /> and <see cref="StringArrayConverter" /> coerce both
///     back into a typed list. These pin that coercion, including the empty and whitespace edges.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ArrayConverterTests
{
    private class ArrayModel
    {
        [JsonProperty("intArray")] public List<int> IntArray { get; set; } = new();
        [JsonProperty("stringArray")] public List<string> StringArray { get; set; } = new();
    }

    private static ArrayModel Coerce(string json) =>
        JsonConvert.DeserializeObject<ArrayModel>(json,
            new JsonSerializerSettings { ContractResolver = new CustomContractResolver() })!;

    [TestMethod]
    public void ContractResolver_ShouldCoerceArrayColumns_GivenJsonArrays()
    {
        var parsed = Coerce("{\"intArray\":[9999,99,99999], \"stringArray\": [\"testing\",\"1\",\"2\"]}");
        parsed.IntArray.Should().Equal(9999, 99, 99999);
        parsed.StringArray.Should().Equal("testing", "1", "2");
    }

    [TestMethod]
    public void ContractResolver_ShouldCoerceArrayColumns_GivenWalrusEncodedStrings()
    {
        // WALRUS delivers array columns as a single Postgres-array string; the converter (not default
        // deserialization) is what turns it back into a list, so this exercises the converter is engaged.
        var parsed = Coerce("{\"intArray\":\"{9999,99,99999}\", \"stringArray\":\"{testing,1,2}\"}");
        parsed.IntArray.Should().Equal(9999, 99, 99999);
        parsed.StringArray.Should().Equal("testing", "1", "2");
    }

    [TestMethod]
    public void IntArrayParse_ShouldReadCurlyForm()
    {
        IntArrayConverter.Parse("{1,2,3}").Should().Equal(1, 2, 3);
    }

    [TestMethod]
    public void IntArrayParse_ShouldReadBracketForm_GivenWhitespace()
    {
        IntArrayConverter.Parse("[1,2,3]").Should().Equal(1, 2, 3);
        IntArrayConverter.Parse("[99, 999, 9999, 999999]").Should().Equal(99, 999, 9999, 999999);
    }

    [TestMethod]
    public void IntArrayParse_ShouldReturnEmpty_GivenEmptyBraces()
    {
        IntArrayConverter.Parse("{}").Should().BeEmpty();
    }

    [TestMethod]
    public void StringArrayParse_ShouldReadBothForms()
    {
        StringArrayConverter.Parse("{a,b,c}").Should().Equal("a", "b", "c");
        StringArrayConverter.Parse("[a,b,c]").Should().Equal("a", "b", "c");
    }
}

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Linq;

/// <summary>
///     How a projection becomes the <c>select</c> query parameter: the LINQ expression overload maps model
///     members to their column names, the string overload passes columns through (stripping whitespace), and
///     a projection that isn't a model column is rejected with an <see cref="ArgumentException" />.
///     Asserted on <c>KitchenSink</c>, which carries no reference joins, so the URL is just the projection.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SelectClauseTests
{
    private const string BaseUrl = "http://localhost:54321/rest/v1";
    private readonly Client client = new(BaseUrl);

    [TestMethod]
    public void Select_ShouldProjectAColumn_GivenAMemberExpression()
    {
        client.Table<KitchenSink>().Select(x => new object[] { x.StringValue! })
            .GenerateUrl().Should().Be($"{BaseUrl}/kitchen_sink?select=string_value");
    }

    [TestMethod]
    public void Select_ShouldProjectMultipleColumns_GivenAMemberExpression()
    {
        client.Table<KitchenSink>().Select(x => new object[] { x.StringValue!, x.IntValue! })
            .GenerateUrl().Should().Be($"{BaseUrl}/kitchen_sink?select=string_value%2cint_value");
    }

    [TestMethod]
    public void Select_ShouldStripWhitespaceAndPassColumnsThrough_GivenTheStringOverload()
    {
        client.Table<KitchenSink>().Select("string_value, int_value")
            .GenerateUrl().Should().Be($"{BaseUrl}/kitchen_sink?select=string_value%2cint_value");
    }

    [TestMethod]
    public void Select_ShouldThrow_GivenAProjectionThatIsNotAColumn()
    {
        var act = () => client.Table<KitchenSink>().Select(x => new object[] { "stringValue" });
        act.Should().Throw<ArgumentException>();
    }
}

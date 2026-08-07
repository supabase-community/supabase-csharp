using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Sorting;

/// <summary>
///     How <c>Order</c> renders onto the <c>order</c> query parameter: <c>column.direction.nullposition</c>,
///     with multiple orderers joined in the order they were added.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class OrderingUrlTests
{
    private const string BaseUrl = "http://localhost:54321/rest/v1";
    private readonly Client client = new(BaseUrl);

    [TestMethod]
    public void Order_ShouldRenderDirectionAndNullsFirstByDefault()
    {
        client.Table<User>().Order("username", Ordering.Descending)
            .GenerateUrl().Should().Be($"{BaseUrl}/users?order=username.desc.nullsfirst");
    }

    [TestMethod]
    public void Order_ShouldRenderNullsLast_GivenNullsLastPosition()
    {
        client.Table<User>().Order("username", Ordering.Ascending, NullPosition.Last)
            .GenerateUrl().Should().Be($"{BaseUrl}/users?order=username.asc.nullslast");
    }

    [TestMethod]
    public void Order_ShouldJoinOrderers_GivenMultipleColumns()
    {
        client.Table<User>()
            .Order("username", Ordering.Descending)
            .Order("status", Ordering.Ascending)
            .GenerateUrl().Should()
            .Be($"{BaseUrl}/users?order=username.desc.nullsfirst%2cstatus.asc.nullsfirst");
    }

    [TestMethod]
    public void Order_ShouldThrow_GivenAProjectionOfMultipleColumns()
    {
        var act = () => client.Table<KitchenSink>()
            .Order(x => new object[] { x.StringValue!, x.IntValue! }, Ordering.Descending);
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Order_ShouldThrow_GivenAnExpressionThatIsNotAColumn()
    {
        var act = () => client.Table<KitchenSink>().Order(x => "something", Ordering.Descending);
        act.Should().Throw<ArgumentException>();
    }
}

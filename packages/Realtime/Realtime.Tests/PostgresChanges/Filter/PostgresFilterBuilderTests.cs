using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime.PostgresChanges.Filter;

namespace Realtime.Tests.PostgresChanges.Filter;

[TestClass]
[TestCategory("Unit")]
public class PostgresFilterBuilderTests
{
    [TestMethod]
    [DataRow("test", "test")]
    [DataRow("\"test\"", "\"\\\"test\\\"\"")]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    [DataRow(true, "true")]
    [DataRow("Hello, World", "\"Hello, World\"")]
    public void Filter_ShouldCreateEqual_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Eq;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Eq(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Filter_ShouldCreateEqualChained_GivenInput()
    {
        const string column1 = "user";
        const string column2 = "account";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Eq;
        var expected =
            $"{column1}={filterOperator.ToMappedString()}.test,{column2}={filterOperator.ToMappedString()}.test";

        var result = PostgresFilterBuilder
            .Builder()
            .Eq(column1, "test")
            .Eq(column2, "test")
            .Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("test", "test")]
    [DataRow("\"test\"", "\"\\\"test\\\"\"")]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    [DataRow(true, "true")]
    [DataRow("Hello, World", "\"Hello, World\"")]
    public void Filter_ShouldCreateNotEqual_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Neq;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Neq(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    public void Filter_ShouldCreateLt_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Lt;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Lt(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    public void Filter_ShouldCreateLte_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Lte;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Lte(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    public void Filter_ShouldCreateGt_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Gt;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Gt(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    public void Filter_ShouldCreateGte_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Gte;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Gte(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(new object[] { "1", "2", "3", "4" }, "(1,2,3,4)")]
    [DataRow(new object[] { "hello", "world" }, "(hello,world)")]
    [DataRow(new object[] { "hello, world", "world" }, "(\"hello, world\",world)")]
    [DataRow(new object[] { 1, 2, 3, 4 }, "(1,2,3,4)")]
    public void Filter_ShouldCreateIn_GivenInput(IEnumerable<object?> value, string expect)
    {
        const string column = "description";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.In;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().In(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("test%", "test%")]
    [DataRow("\"%test\"", "\"\\\"%test\\\"\"")]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    [DataRow(true, "true")]
    [DataRow("%Hello, World%", "\"%Hello, World%\"")]
    public void Filter_ShouldCreateLike_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Like;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Like(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("test%", "test%")]
    [DataRow("\"%test\"", "\"\\\"%test\\\"\"")]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    [DataRow(true, "true")]
    [DataRow("%Hello, World%", "\"%Hello, World%\"")]
    public void Filter_ShouldCreateILike_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.ILike;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().ILike(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("test%", "test%")]
    [DataRow("\"%test\"", "\"\\\"%test\\\"\"")]
    [DataRow(2, "2")]
    [DataRow(2D, "2")]
    [DataRow(2F, "2")]
    [DataRow(true, "true")]
    [DataRow("%Hello, World%", "\"%Hello, World%\"")]
    public void Filter_ShouldCreateNot_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.ILike;
        var expected = $"{column}=not.{filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Not(column, filterOperator, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(new object[] { "1", "2", "3", "4" }, "(1,2,3,4)")]
    [DataRow(new object[] { "hello", "world" }, "(hello,world)")]
    [DataRow(new object[] { "hello, world", "world" }, "(\"hello, world\",world)")]
    [DataRow(new object[] { 1, 2, 3, 4 }, "(1,2,3,4)")]
    public void Filter_ShouldCreateNotIn_GivenInput(IEnumerable<object?> value, string expect)
    {
        const string column = "description";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.In;
        var expected = $"{column}=not.{filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Not(column, filterOperator, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("null", "null")]
    [DataRow(null, "null")]
    [DataRow(true, "true")]
    [DataRow(false, "false")]
    public void Filter_ShouldCreateIs_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Is;
        var expected = $"{column}={filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Is(column, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("null", "null")]
    [DataRow(null, "null")]
    [DataRow(true, "true")]
    [DataRow(false, "false")]
    public void Filter_ShouldCreateNotIs_GivenInput(object value, string expect)
    {
        const string column = "user";
        const PostgresChangesFilterOperator filterOperator = PostgresChangesFilterOperator.Is;
        var expected = $"{column}=not.{filterOperator.ToMappedString()}.{expect}";

        var result = PostgresFilterBuilder.Builder().Not(column, filterOperator, value).Build();

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Filter_ShouldThrow_GivenInWithString()
    {
        Assert.Throws<ArgumentException>(() =>
            PostgresFilterBuilder
                .Builder()
                .Not("status", PostgresChangesFilterOperator.In, "open")
                .Build()
        );
    }

    [TestMethod]
    public void Filter_ShouldThrow_GivenInWithEmptyCollection()
    {
        Assert.Throws<ArgumentException>(() =>
            PostgresFilterBuilder.Builder().In("status", Array.Empty<object?>()).Build()
        );
    }

    [TestMethod]
    public void Filter_ShouldThrow_GivenIsWithInvalidString()
    {
        Assert.Throws<ArgumentException>(() =>
            PostgresFilterBuilder.Builder().Is("status", "open").Build()
        );
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Interfaces;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Filtering;

/// <summary>
///     The <see cref="QueryFilter" /> constructors are the SDK's guard rail: each overload accepts only the
///     operators it can render, coerces temporal criteria to a round-trippable wire string, and rejects
///     everything else with an <see cref="Operator" />-specific message. These pin that construction contract.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class QueryFilterConstructionTests
{
    [TestMethod]
    public void Constructor_ShouldFormatDateTimeCriteriaAsRoundTripString()
    {
        var moment = new DateTime(2022, 8, 20, 13, 5, 0, DateTimeKind.Utc);
        var filter = new QueryFilter("created_at", Operator.GreaterThan, moment);
        filter.Criteria.Should().Be(moment.ToString("o", CultureInfo.InvariantCulture),
            "temporal criteria are sent as ISO-8601 round-trip ('o') so the server parses the exact instant");
    }

    [TestMethod]
    public void Constructor_ShouldFormatDateTimeOffsetCriteriaAsRoundTripString()
    {
        var moment = new DateTimeOffset(2022, 8, 20, 13, 5, 0, TimeSpan.FromHours(2));
        var filter = new QueryFilter("created_at", Operator.LessThan, moment);
        filter.Criteria.Should().Be(moment.ToString("o", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Constructor_ShouldRetainScalarCriteria_GivenValueOperator()
    {
        var filter = new QueryFilter("username", Operator.Equals, "supabot");
        filter.Property.Should().Be("username");
        filter.Op.Should().Be(Operator.Equals);
        filter.Criteria.Should().Be("supabot");
    }

    [TestMethod]
    public void Constructor_ShouldRetainListCriteria_GivenArrayOperator()
    {
        var values = new List<object> { "supabot", "kiwicopple" };
        var filter = new QueryFilter("username", Operator.In, values);
        filter.Criteria.Should().BeSameAs(values);
    }

    [TestMethod]
    public void Constructor_ShouldReject_GivenArrayOperatorWithScalarCriteria()
    {
        var act = () => new QueryFilter("username", Operator.In, "supabot");
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*List or Dictionary*");
    }

    [TestMethod]
    public void Constructor_ShouldReject_GivenOperatorUnsupportedByTheScalarOverload()
    {
        var act = () => new QueryFilter("username", Operator.FTS, "supabot");
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*Advanced filters*");
    }

    [TestMethod]
    public void FullTextSearchConstructor_ShouldReject_GivenNonSearchOperator()
    {
        var act = () => new QueryFilter("catchphrase", Operator.Equals, new FullTextSearchConfig("cat", "english"));
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*full text search*");
    }

    [TestMethod]
    public void RangeConstructor_ShouldReject_GivenOperatorThatDoesNotAcceptARange()
    {
        var act = () => new QueryFilter("age_range", Operator.Equals, new IntRange(1, 2));
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*accepts a range*");
    }

    [TestMethod]
    public void LogicalConstructor_ShouldReject_GivenOperatorThatIsNotAndOr()
    {
        var act = () => new QueryFilter(Operator.Equals, new List<IPostgrestQueryFilter>());
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*`or` or `and`*");
    }

    [TestMethod]
    public void NotConstructor_ShouldReject_GivenOperatorThatIsNotNot()
    {
        var inner = new QueryFilter("username", Operator.Equals, "supabot");
        var act = () => new QueryFilter(Operator.And, inner);
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*`not` filter*");
    }

    [TestMethod]
    public void FullTextSearchConfig_ShouldDefaultToEnglish_GivenNoConfig()
    {
        var config = new FullTextSearchConfig("cat", null);
        config.Config.Should().Be("english",
            "an absent language config must fall back to english rather than an empty tsquery config");
    }

    [TestMethod]
    public void FullTextSearchConfig_ShouldRetainConfig_GivenExplicitLanguage()
    {
        var config = new FullTextSearchConfig("cat", "french");
        config.Config.Should().Be("french");
    }
}

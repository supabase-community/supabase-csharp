using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Converters;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Extensions;

namespace Postgrest.Tests.Serialization;

/// <summary>
///     Round-tripping a Postgres <c>int4range</c>: <see cref="RangeConverter.ParseIntRange" /> reads the
///     bracket/paren bound notation (per the Postgres range docs) and <c>ToPostgresString</c> writes an
///     inclusive <c>[start,end]</c> string the server understands.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class RangeSerializationTests
{
    [TestMethod]
    public void ParseIntRange_ShouldNormalizeExclusiveUpperBoundToInclusive()
    {
        var range = RangeConverter.ParseIntRange("[3,7)");
        using (new AssertionScope())
        {
            range.Start.Value.Should().Be(3, "'[' keeps the lower bound inclusive");
            range.End.Value.Should().Be(6, "')' drops the exclusive upper bound by one");
        }
    }

    [TestMethod]
    public void ParseIntRange_ShouldNormalizeExclusiveLowerBoundToInclusive()
    {
        var range = RangeConverter.ParseIntRange("(3,7)");
        using (new AssertionScope())
        {
            range.Start.Value.Should().Be(4, "'(' bumps the exclusive lower bound up by one");
            range.End.Value.Should().Be(6);
        }
    }

    [TestMethod]
    public void ParseIntRange_ShouldPreserveFullyInclusiveBounds()
    {
        var range = RangeConverter.ParseIntRange("[4,4]");
        using (new AssertionScope())
        {
            range.Start.Value.Should().Be(4);
            range.End.Value.Should().Be(4);
        }
    }

    [TestMethod]
    public void ParseIntRange_ShouldCollapseToEmpty_GivenABoundThatIncludesNoPoints()
    {
        var range = RangeConverter.ParseIntRange("[4,4)");
        using (new AssertionScope())
        {
            range.Start.Value.Should().Be(0);
            range.End.Value.Should().Be(0);
        }
    }

    [TestMethod]
    public void ParseIntRange_ShouldReject_GivenNonIntegerBounds()
    {
        var act = () => RangeConverter.ParseIntRange("[1.2,3]");
        act.Should().Throw<PostgrestException>()
            .Where(exception => exception.Reason == FailureHint.Reason.InvalidArgument)
            .WithMessage("*Unknown Range format*");
    }

    [TestMethod]
    public void ToPostgresString_ShouldRenderInclusiveBrackets()
    {
        using (new AssertionScope())
        {
            new IntRange(1, 7).ToPostgresString().Should().Be("[1,7]");
            new IntRange(4, 6).ToPostgresString().Should().Be("[4,6]");
        }
    }
}

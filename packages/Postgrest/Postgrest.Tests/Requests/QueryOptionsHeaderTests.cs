using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;

namespace Postgrest.Tests.Requests;

/// <summary>
///     <see cref="QueryOptions.ToHeaders" /> composes the PostgREST <c>Prefer</c> header from the return,
///     upsert-resolution and count choices. These pin how each option contributes to that comma-joined value.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class QueryOptionsHeaderTests
{
    [TestMethod]
    public void ToHeaders_ShouldRequestRepresentationByDefault() =>
        new QueryOptions().ToHeaders()["Prefer"].Should().Be("return=representation");

    [TestMethod]
    public void ToHeaders_ShouldRequestMinimal_GivenMinimalReturn() =>
        new QueryOptions { Returning = QueryOptions.ReturnType.Minimal }
            .ToHeaders()["Prefer"].Should().Be("return=minimal");

    [TestMethod]
    public void ToHeaders_ShouldPrependMergeResolution_GivenUpsert() =>
        new QueryOptions { Returning = QueryOptions.ReturnType.Minimal, Upsert = true }
            .ToHeaders()["Prefer"].Should().Be("resolution=merge-duplicates,return=minimal");

    [TestMethod]
    public void ToHeaders_ShouldUseIgnoreResolution_GivenIgnoreDuplicates() =>
        new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Minimal,
            Upsert = true,
            DuplicateResolution = QueryOptions.DuplicateResolutionType.IgnoreDuplicates
        }.ToHeaders()["Prefer"].Should().Be("resolution=ignore-duplicates,return=minimal");

    [TestMethod]
    public void ToHeaders_ShouldAppendCount_GivenACountAlgorithm() =>
        new QueryOptions { Returning = QueryOptions.ReturnType.Minimal, Count = QueryOptions.CountType.Exact }
            .ToHeaders()["Prefer"].Should().Be("return=minimal,count=exact");
}

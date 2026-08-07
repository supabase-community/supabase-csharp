using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest.Extensions;

namespace Postgrest.Tests.Extensions;

/// <summary>
///     <see cref="UriExtensions.GetInstanceUrl" /> strips the query string, leaving the scheme, host and path
///     that identify the instance — the form recorded in telemetry and used to compose requests.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class UriExtensionsTests
{
    [TestMethod]
    public void GetInstanceUrl_ShouldDropTheQueryString()
    {
        new Uri("https://abcdefg.supabase.io/rest/v1?query=me-big-query").GetInstanceUrl()
            .Should().Be("https://abcdefg.supabase.io/rest/v1");
    }

    [TestMethod]
    public void GetInstanceUrl_ShouldPreserveHostAndPath_GivenANonDefaultPort()
    {
        new Uri("http://localhost:3000/testing/123?query=me-big-query").GetInstanceUrl()
            .Should().Be("http://localhost:3000/testing/123");
    }
}

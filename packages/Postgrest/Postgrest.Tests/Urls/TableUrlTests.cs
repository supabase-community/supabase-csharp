using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Urls;

/// <summary>
///     How a bare table query renders its URL before any filters: the table name comes from
///     <c>[Table]</c> (falling back to the class name), and client-level query params and an <c>apikey</c>
///     header are threaded onto the query string.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class TableUrlTests
{
    private const string BaseUrl = "http://localhost:54321/rest/v1";

    [TestMethod]
    public void Constructor_ShouldExposeTheBaseUrl() =>
        new Client(BaseUrl).BaseUrl.Should().Be(BaseUrl);

    [TestMethod]
    public void GenerateUrl_ShouldUseTheTableAttributeName() =>
        new Client(BaseUrl).Table<User>().GenerateUrl().Should().Be($"{BaseUrl}/users");

    [TestMethod]
    public void GenerateUrl_ShouldFallBackToTheClassName_GivenNoTableAttribute() =>
        new Client(BaseUrl).Table<Stub>().GenerateUrl().Should().Be($"{BaseUrl}/Stub");

    [TestMethod]
    public void GenerateUrl_ShouldAppendClientQueryParams()
    {
        var client = new Client(BaseUrl, new ClientOptions
        {
            QueryParams = new Dictionary<string, string> { { "some-param", "foo" }, { "other-param", "bar" } }
        });
        client.Table<User>().GenerateUrl().Should().Be($"{BaseUrl}/users?some-param=foo&other-param=bar");
    }

    [TestMethod]
    public void GenerateUrl_ShouldAppendApiKey_GivenAnApiKeyHeader()
    {
        var client = new Client(BaseUrl, new ClientOptions { Headers = { { "apikey", "some-key" } } });
        client.Table<User>().GenerateUrl().Should().Be($"{BaseUrl}/users?apikey=some-key");
    }
}

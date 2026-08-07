using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Attaching;

/// <summary>
///     <c>Client.Attach</c> hydrates a model that was created outside a request (e.g. by Realtime) with the
///     client context it needs to issue its own updates — base URL, options and the header provider — and
///     returns the same instance so callers can chain.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AttachTests
{
    private const string BaseUrl = "http://localhost:54321/rest/v1";

    [TestMethod]
    public void BareModel_ShouldHaveNoClientContext()
    {
        var model = new Movie { Name = "Realtime-hydrated Movie" };
        using (new AssertionScope())
        {
            model.BaseUrl.Should().BeNull();
            model.RequestClientOptions.Should().BeNull();
            model.GetHeaders.Should().BeNull();
        }
    }

    [TestMethod]
    public void Attach_ShouldPopulateClientContext()
    {
        var options = new ClientOptions { Schema = "public" };
        var client = new Client(BaseUrl, options)
        {
            GetHeaders = () => new Dictionary<string, string> { { "Authorization", "Bearer test" } }
        };
        var result = client.Attach(new Movie { Name = "Realtime-hydrated Movie" });
        using (new AssertionScope())
        {
            result.BaseUrl.Should().Be(BaseUrl);
            result.RequestClientOptions.Should().BeSameAs(options);
            result.GetHeaders.Should().BeSameAs(client.GetHeaders);
        }
    }

    [TestMethod]
    public void Attach_ShouldReturnTheSameInstanceForChaining()
    {
        var client = new Client(BaseUrl, new ClientOptions { Schema = "public" });
        var model = new Movie { Name = "Realtime-hydrated Movie" };
        client.Attach(model).Should().BeSameAs(model);
    }
}

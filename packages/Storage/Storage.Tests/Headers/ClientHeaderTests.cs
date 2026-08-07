using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace Storage.Tests.Headers;

/// <summary>
/// Covers how <see cref="Client"/> composes its outgoing headers: values from the dynamic
/// <see cref="StorageBucketApi.GetHeaders"/> callback are merged in, but the headers supplied at
/// construction always win, and assigning the header set stamps the <c>X-Client-Info</c> version tag.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ClientHeaderTests
{
    [TestMethod]
    public void Headers_ShouldIncludeDynamicHeaders_GivenGetHeaders()
    {
        var client = new Client("http://localhost:5000", new Dictionary<string, string> { { "Testing", "1234" } })
        {
            GetHeaders = () => new Dictionary<string, string> { { "Dynamic", "4567" } }
        };
        client.Headers.Should().Contain("Dynamic", "4567");
    }

    [TestMethod]
    public void Headers_ShouldPreferConstructorHeadersOverDynamic()
    {
        var client = new Client("http://localhost:5000", new Dictionary<string, string> { { "Testing", "1234" } })
        {
            GetHeaders = () => new Dictionary<string, string> { { "Testing", "4567" } }
        };
        client.Headers.Should().Contain("Testing", "1234");
    }

    [TestMethod]
    public void Headers_ShouldStampClientInfoWhenAssigned()
    {
        var client = new Client("http://localhost:5000")
        {
            Headers = new Dictionary<string, string> { { "Testing", "1234" } }
        };
        client.Headers.Should().ContainKey("X-Client-Info");
    }
}

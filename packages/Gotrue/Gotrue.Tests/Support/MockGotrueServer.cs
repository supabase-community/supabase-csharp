#region

using System;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.Server;

#endregion

namespace Gotrue.Tests.Support;

/// <summary>
///     A hermetic stand-in for the GoTrue HTTP API: Contract-tier tests point a real <c>Client</c> at this
///     WireMock server, stub the endpoints they exercise, and assert the exact request the SDK put on the wire.
/// </summary>
internal sealed class MockGotrueServer : IDisposable
{
    internal const string ApiKey = "test-api-key";

    private readonly WireMockServer server = WireMockServer.Start();

    internal string Url => server.Url!;

    public void Dispose() => server.Stop();

    internal IRespondWithAProvider Given(IRequestBuilder requestBuilder) => server.Given(requestBuilder);

    internal void Reset() => server.ResetMappings();

    internal ReceivedRequest VerifySingleReceivedRequest()
    {
        var entry = server.LogEntries.Should().ContainSingle("the SDK should emit exactly one request").Which;
        return new ReceivedRequest(entry.RequestMessage!);
    }
}

/// <summary>
///     Fluent assertions over a request the SDK sent to the <see cref="MockGotrueServer" />.
/// </summary>
internal sealed class ReceivedRequest
{
    private readonly IRequestMessage request;

    internal ReceivedRequest(IRequestMessage request) => this.request = request;

    internal ReceivedRequest WithPath(string path)
    {
        request.Path.Should().Be(path);
        return this;
    }

    internal ReceivedRequest WithMethod(HttpMethod method)
    {
        request.Method.Should().Be(method.ToString());
        return this;
    }

    internal ReceivedRequest WithQueryParam(string name, string expected)
    {
        request.Query.Should().ContainKey(name).WhoseValue.Single().Should().Be(expected);
        return this;
    }

    internal ReceivedRequest WithHeader(string name, string expected)
    {
        request.Headers.Should().ContainKey(name).WhoseValue.Single().Should().Be(expected);
        return this;
    }

    internal ReceivedRequest WithJsonContentType()
    {
        request.Headers.Should().ContainKey("Content-Type").WhoseValue.Single().Should().StartWith("application/json");
        return this;
    }

    internal ReceivedRequest WithExactJsonBody(string field, string expected)
    {
        request.Body.Should().NotBeNull("the request should have a body");
        var body = JObject.Parse(request.Body!);
        body[field]?.Value<string>().Should().Be(expected);
        return this;
    }

    internal ReceivedRequest WithBooleanJsonBody(string field, bool expected)
    {
        request.Body.Should().NotBeNull("the request should have a body");
        var body = JObject.Parse(request.Body!);
        body[field]?.Value<bool>().Should().Be(expected);
        return this;
    }

    internal ReceivedRequest WithNestedJsonBody(string parent, string field, string expected)
    {
        request.Body.Should().NotBeNull("the request should have a body");
        var body = JObject.Parse(request.Body!);
        body[parent]?[field]?.Value<string>().Should().Be(expected);
        return this;
    }

    internal ReceivedRequest WithoutJsonBodyField(string field)
    {
        request.Body.Should().NotBeNull("the request should have a body");
        var body = JObject.Parse(request.Body!);
        body.ContainsKey(field).Should().BeFalse($"'{field}' should be omitted when not supplied");
        return this;
    }

    internal string? ReadJsonBodyField(string field)
    {
        request.Body.Should().NotBeNull("the request should have a body");
        var body = JObject.Parse(request.Body!);
        return body[field]?.Value<string>();
    }
}

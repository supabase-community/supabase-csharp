using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Realtime;
using Supabase.Realtime.Channel;

namespace Realtime.Tests.Channels;

/// <summary>
///     The public/private distinction a channel carries: <see cref="ChannelOptions.Private" /> marks the
///     channel as authorized against Row Level Security (required for broadcast replay), and the access-token
///     accessor is surfaced back to callers.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ChannelOptionsTests
{
    private static readonly JsonSerializerSettings Settings = new();

    [TestMethod]
    public void Public_ShouldNotBePrivate()
    {
        ChannelOptions.Public(new ClientOptions(), () => null, Settings).IsPrivate.Should().BeFalse();
    }

    [TestMethod]
    public void Private_ShouldBePrivate()
    {
        ChannelOptions.Private(new ClientOptions(), () => null, Settings).IsPrivate.Should().BeTrue();
    }

    [TestMethod]
    public void Constructor_ShouldDefaultToPublic()
    {
        new ChannelOptions(new ClientOptions(), () => null, Settings).IsPrivate.Should().BeFalse();
    }

    [TestMethod]
    public void RetrieveAccessToken_ShouldReturnConfiguredToken()
    {
        var options = ChannelOptions.Public(new ClientOptions(), () => "jwt-123", Settings);
        options.RetrieveAccessToken().Should().Be("jwt-123");
    }
}

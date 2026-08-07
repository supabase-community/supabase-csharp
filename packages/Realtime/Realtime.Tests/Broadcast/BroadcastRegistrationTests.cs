using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Models;
using Realtime.Tests.Support;
using Supabase.Realtime.Broadcast;

namespace Realtime.Tests.Broadcast;

/// <summary>
///     Client-side rules a channel enforces when registering for broadcast, before any server is involved:
///     broadcast can only be registered once, and replay-from-history requires a private channel.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class BroadcastRegistrationTests
{
    private static BroadcastOptions WithReplay() => new()
    {
        Replay = new BroadcastOptions.ReplayOptions { Limit = 10, Since = 0 }
    };

    [TestMethod]
    public void Register_ShouldThrow_GivenReplayOnPublicChannel()
    {
        var channel = Wire.Channel(options: Wire.PublicOptions());
        var act = () => channel.Register<BroadcastExample>(WithReplay());
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Register_ShouldSucceed_GivenReplayOnPrivateChannel()
    {
        var channel = Wire.Channel(options: Wire.PrivateOptions());
        channel.Register<BroadcastExample>(WithReplay()).Should().NotBeNull();
    }

    [TestMethod]
    public void Register_ShouldThrow_GivenCalledTwice()
    {
        var channel = Wire.Channel();
        channel.Register<BroadcastExample>();
        var act = () => channel.Register<BroadcastExample>();
        act.Should().Throw<InvalidOperationException>();
    }
}

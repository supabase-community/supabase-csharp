using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Models;
using Realtime.Tests.Support;
using Supabase.Realtime.Socket;

namespace Realtime.Tests.Broadcast;

/// <summary>
///     What a channel does with an inbound <c>broadcast</c> frame: it routes it to the registered broadcast
///     instance, which types the payload and hands it to listeners. Driven through the channel's public
///     message entry point over a fake socket, so no live server is needed.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class BroadcastMessageTests
{
    private const string Frame =
        "{\"topic\":\"realtime:example\",\"event\":\"broadcast\",\"ref\":\"srv-1\",\"payload\":{\"event\":\"user\",\"userId\":\"abc-123\"}}";

    [TestMethod]
    public void HandleSocketMessage_ShouldDeliverTypedBroadcastToListener()
    {
        var channel = Wire.Channel();
        var broadcast = channel.Register<BroadcastExample>();
        BroadcastExample? received = null;
        broadcast.AddBroadcastEventHandler((_, _) => received = broadcast.Current());
        channel.HandleSocketMessage(Wire.Decode(Frame));
        received.Should().NotBeNull();
        received!.UserId.Should().Be("abc-123");
        received.Event.Should().Be("user");
    }

    [TestMethod]
    public void Current_ShouldReturnNull_GivenNoBroadcastReceived()
    {
        var channel = Wire.Channel();
        var broadcast = channel.Register<BroadcastExample>();
        broadcast.Current().Should().BeNull();
    }

    [TestMethod]
    public void TriggerReceived_ShouldThrow_GivenUnparsableResponse()
    {
        var channel = Wire.Channel();
        var broadcast = channel.Register<BroadcastExample>();
        var act = () => broadcast.TriggerReceived(new SocketResponse(Wire.Settings()));
        act.Should().Throw<ArgumentException>();
    }
}

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Support;
using Supabase.Realtime.Exceptions;
using static Supabase.Realtime.Constants;

namespace Realtime.Tests.Channels;

/// <summary>
///     A channel's join lifecycle over an unconnected socket: pushing before a join is refused, and
///     unsubscribing walks state through <c>Leaving</c> to <c>Closed</c>. The join handshake itself needs a
///     server reply and is covered in the E2E tier.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ChannelLifecycleTests
{
    [TestMethod]
    public void Push_ShouldThrow_GivenNotJoined()
    {
        var channel = Wire.Channel();
        var act = () => channel.Push("some_event");
        act.Should().Throw<RealtimeException>().Which.Reason.Should().Be(FailureHint.Reason.ChannelNotOpen);
    }

    [TestMethod]
    public void Unsubscribe_ShouldWalkStateToClosed()
    {
        var channel = Wire.Channel();
        var states = new List<ChannelState>();
        channel.AddStateChangedHandler((_, state) => states.Add(state));
        channel.Unsubscribe();
        states.Should().ContainInOrder(ChannelState.Leaving, ChannelState.Closed);
        channel.IsClosed.Should().BeTrue();
    }
}

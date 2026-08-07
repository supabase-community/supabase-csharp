using System;
using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime.Exceptions;
using Websocket.Client;

namespace Realtime.Tests.Errors;

/// <summary>
///     How a transport disconnection is classified into a <see cref="FailureHint.Reason" /> and surfaced on a
///     <see cref="RealtimeException" />, so a caller can branch on <em>why</em> a socket dropped.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class FailureHintTests
{
    private static DisconnectionInfo Info(DisconnectionType type, string? description = "dropped", Exception? exception = null) =>
        new(type, WebSocketCloseStatus.NormalClosure, description, subProtocol: null, exception);

    [TestMethod]
    [DataRow(DisconnectionType.Error, FailureHint.Reason.SocketError)]
    [DataRow(DisconnectionType.NoMessageReceived, FailureHint.Reason.ConnectionStale)]
    [DataRow(DisconnectionType.Lost, FailureHint.Reason.ConnectionLost)]
    [DataRow(DisconnectionType.ByServer, FailureHint.Reason.Unknown)]
    [DataRow(DisconnectionType.ByUser, FailureHint.Reason.Unknown)]
    public void Parse_ShouldClassifyDisconnectionType(DisconnectionType type, FailureHint.Reason expected)
    {
        FailureHint.Parse(Info(type)).Should().Be(expected);
    }

    [TestMethod]
    public void FromDisconnectionInfo_ShouldCarryReasonMessageAndInnerException()
    {
        var inner = new InvalidOperationException("boom");
        var exception = RealtimeException.FromDisconnectionInfo(Info(DisconnectionType.Error, "socket died", inner));
        exception.Reason.Should().Be(FailureHint.Reason.SocketError);
        exception.Message.Should().Be("socket died");
        exception.InnerException.Should().BeSameAs(inner);
    }
}

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Realtime.Tests.Support;
using Supabase.Realtime.Socket;
using static Supabase.Realtime.Constants;

namespace Realtime.Tests.Serialization;

/// <summary>
///     How a raw Phoenix frame maps onto the typed <see cref="SocketResponse.Event" /> the channel switches
///     on, and how a postgres_changes payload's <c>type</c> and <c>errors</c> fields deserialize. These pin
///     the wire contract the whole dispatch path depends on.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SocketResponseSerializationTests
{
    [TestMethod]
    [DataRow(ChannelEventPresenceState, EventType.PresenceState)]
    [DataRow(ChannelEventPresenceDiff, EventType.PresenceDiff)]
    [DataRow(ChannelEventBroadcast, EventType.Broadcast)]
    [DataRow(ChannelEventPostgresChanges, EventType.PostgresChanges)]
    [DataRow(ChannelEventSystem, EventType.System)]
    [DataRow(ChannelEventReply, EventType.PostgresChanges)]
    public void Event_ShouldMapPhoenixEventNameToTypedEvent(string phoenixEvent, EventType expected)
    {
        var response = Wire.Decode($"{{\"topic\":\"realtime:x\",\"event\":\"{phoenixEvent}\",\"ref\":\"1\"}}");
        response.Event.Should().Be(expected);
    }

    [TestMethod]
    public void Event_ShouldFallBackToPayloadType_GivenUnrecognisedEventName()
    {
        var response = Wire.Decode("{\"topic\":\"realtime:x\",\"event\":\"unhandled\",\"payload\":{\"type\":\"INSERT\"},\"ref\":\"1\"}");
        response.Event.Should().Be(EventType.Insert);
    }

    [TestMethod]
    public void Event_ShouldBeUnknown_GivenNoEventAndNoPayloadType()
    {
        var response = Wire.Decode("{\"topic\":\"realtime:x\",\"event\":\"unhandled\",\"ref\":\"1\"}");
        response.Event.Should().Be(EventType.Unknown);
    }

    [TestMethod]
    [DataRow("INSERT", EventType.Insert)]
    [DataRow("UPDATE", EventType.Update)]
    [DataRow("DELETE", EventType.Delete)]
    [DataRow("TRUNCATE", EventType.Unknown)]
    public void PayloadType_ShouldMapActionString(string action, EventType expected)
    {
        var payload = JsonConvert.DeserializeObject<SocketResponsePayload>($"{{\"type\":\"{action}\"}}");
        payload!.Type.Should().Be(expected);
    }

    [TestMethod]
    public void PayloadErrors_ShouldDeserialize_GivenPresent()
    {
        const string json =
            "{\"schema\":\"public\",\"table\":\"todos\",\"type\":\"UPDATE\",\"errors\":[\"Error 413: Payload Too Large\"]}";
        var payload = JsonConvert.DeserializeObject<SocketResponsePayload>(json);
        payload!.Errors.Should().ContainSingle().Which.Should().Be("Error 413: Payload Too Large");
    }

    [TestMethod]
    public void PayloadErrors_ShouldBeNull_GivenAbsent()
    {
        const string json = "{\"schema\":\"public\",\"table\":\"todos\",\"type\":\"UPDATE\",\"errors\":null}";
        var payload = JsonConvert.DeserializeObject<SocketResponsePayload>(json);
        payload!.Errors.Should().BeNull();
    }
}

using System.Text.Json;
using Supabase.Realtime;
using Supabase.Realtime.Socket;
using VerifyMSTest;

namespace Realtime.Tests.Support;

/// <summary>
///     Base fixture for outbound-frame approval (Contract-tier) tests. Realtime speaks WebSocket, not HTTP,
///     so there is no request body for WireMock to capture; the wire boundary is instead the client's default
///     encoder — <c>JsonConvert.SerializeObject(frame, SerializerSettings)</c> — which is what gets written to
///     the socket. <see cref="Encode" /> runs that exact production serializer (the <c>CustomContractResolver</c>
///     and date/array converters) over a <see cref="SocketRequest" />, so the snapshot is the literal frame the
///     SDK would send. This is the transport contract the System.Text.Json migration must preserve.
/// </summary>
public abstract class FrameApprovalFixture : VerifyBase
{
    private readonly Client client = new("ws://localhost:4000/realtime/v1");

    /// <summary>Serializes a socket frame exactly as the client encodes it before writing to the websocket.</summary>
    protected string Encode(SocketRequest frame) =>
        JsonSerializer.Serialize(frame, this.client.SerializerSettings);
}

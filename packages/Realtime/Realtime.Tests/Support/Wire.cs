using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Supabase.Realtime;
using Supabase.Realtime.Channel;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Socket;

namespace Realtime.Tests.Support;

/// <summary>
///     Shared builders for the hermetic channel tests. The seam is a real, <em>unconnected</em>
///     <see cref="RealtimeSocket" />: constructing it opens no connection, so a channel can be exercised in
///     process without a live Phoenix server and without a hand-written stub. Inbound routing is driven by
///     calling <c>RealtimeChannel.HandleSocketMessage</c> with a frame from <see cref="Decode" />; the
///     subscribe/send handshake (which needs the server to reply) stays in the E2E tier.
/// </summary>
internal static class Wire
{
    private const string Endpoint = "ws://127.0.0.1:54321/realtime/v1";

    public static JsonSerializerSettings Settings() => new()
    {
        ContractResolver = new CustomContractResolver(),
        Converters = { new IsoDateTimeConverter { DateTimeFormat = @"yyyy'-'MM'-'dd' 'HH':'mm':'ss.FFFFFFK" } },
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public static ChannelOptions PublicOptions() =>
        ChannelOptions.Public(new ClientOptions(), () => null, Settings());

    public static ChannelOptions PrivateOptions() =>
        ChannelOptions.Private(new ClientOptions(), () => null, Settings());

    public static IRealtimeSocket Socket() => new RealtimeSocket(Endpoint, new ClientOptions());

    public static RealtimeChannel Channel(string topic = "realtime:example", ChannelOptions? options = null) =>
        new(Socket(), topic, options ?? PublicOptions());

    /// <summary>
    ///     Reproduces the client's default decoder: populate a <see cref="SocketResponse" /> from the frame and
    ///     stamp the raw JSON, which the typed re-parsing in broadcast/presence/postgres_changes relies on.
    /// </summary>
    public static SocketResponse Decode(string json)
    {
        var settings = Settings();
        var response = new SocketResponse(settings);
        JsonConvert.PopulateObject(json, response, settings);
        response.Json = json;
        return response;
    }
}

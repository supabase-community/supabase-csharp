using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Models;
using Realtime.Tests.Support;
using Supabase.Postgrest.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Broadcast;
using Supabase.Realtime.Channel;
using Supabase.Realtime.Interfaces;

namespace Realtime.Tests.Broadcast;

/// <summary>
///     Broadcast against the live stack: messages relay between two clients, work on private (RLS-authorized)
///     channels, replay from history on a private channel, and a send resolves even when no server ack was
///     configured (supabase-community/realtime-csharp#38).
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class BroadcastRelayTests
{
    private IPostgrestClient restClient = null!;
    private IRealtimeClient<RealtimeSocket, RealtimeChannel> socketClient = null!;

    [TestInitialize]
    public async Task InitializeTest()
    {
        this.restClient = Helpers.RestClient();
        this.socketClient = Helpers.SocketClient();
        await this.socketClient.ConnectAsync();
    }

    [TestCleanup]
    public void CleanupTest() => this.socketClient.Disconnect();

    [TestMethod]
    public async Task Broadcast_ShouldRelayBetweenClients()
    {
        var tsc = new TaskCompletionSource<bool>();
        var tsc2 = new TaskCompletionSource<bool>();
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var topic = $"online-users-{Guid.NewGuid()}";
        var channel1 = this.socketClient.Channel(topic);
        var broadcast1 = channel1.Register<BroadcastExample>(true, true);
        broadcast1.AddBroadcastEventHandler((_, _) =>
        {
            var broadcast = broadcast1.Current();
            if (broadcast?.UserId != guid1 && broadcast?.Event == "user")
                tsc.TrySetResult(true);
        });
        var client2 = Helpers.SocketClient();
        await client2.ConnectAsync();
        var channel2 = client2.Channel(topic);
        var broadcast2 = channel2.Register<BroadcastExample>(true, true);
        broadcast2.AddBroadcastEventHandler((_, _) =>
        {
            var broadcast = broadcast2.Current();
            if (broadcast?.UserId != guid2 && broadcast?.Event == "user")
                tsc2.TrySetResult(true);
        });
        await channel1.Subscribe();
        await channel2.Subscribe();
        await broadcast1.Send("user", new BroadcastExample { UserId = guid1 });
        await broadcast2.Send("user", new BroadcastExample { UserId = guid2 });
        await Task.WhenAll(tsc.Task, tsc2.Task);
    }

    [TestMethod]
    public async Task Broadcast_ShouldRelayOnPrivateChannel()
    {
        var tsc = new TaskCompletionSource<bool>();
        var tsc2 = new TaskCompletionSource<bool>();
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var topic = $"online-users-{Guid.NewGuid()}";
        var client1 = Helpers.PrivateSocketClient();
        await client1.ConnectAsync();
        var channel1 = client1.Channel(topic,
            ChannelOptions.Private(client1.Options, () => Helpers.ApiKey, Wire.Settings()));
        var broadcast1 = channel1.Register<BroadcastExample>(true, true);
        broadcast1.AddBroadcastEventHandler((_, _) =>
        {
            var broadcast = broadcast1.Current();
            if (broadcast?.UserId != guid1 && broadcast?.Event == "user")
                tsc.TrySetResult(true);
        });
        var client2 = Helpers.PrivateSocketClient();
        await client2.ConnectAsync();
        var channel2 = client2.Channel(topic,
            ChannelOptions.Private(client2.Options, () => Helpers.ApiKey, Wire.Settings()));
        var broadcast2 = channel2.Register<BroadcastExample>(true, true);
        broadcast2.AddBroadcastEventHandler((_, _) =>
        {
            var broadcast = broadcast2.Current();
            if (broadcast?.UserId != guid2 && broadcast?.Event == "user")
                tsc2.TrySetResult(true);
        });
        await channel1.Subscribe();
        await channel2.Subscribe();
        await broadcast1.Send("user", new BroadcastExample { UserId = guid1 });
        await broadcast2.Send("user", new BroadcastExample { UserId = guid2 });
        await Task.WhenAll(tsc.Task, tsc2.Task);
    }

    [TestMethod]
    public async Task Send_ShouldResolve_GivenNoExplicitAck()
    {
        // Mirrors the most natural usage: Channel(name) -> Subscribe() -> Send(), with no
        // Register<T>(broadcastAck: true). See supabase-community/realtime-csharp#38.
        var channel = this.socketClient.Channel("no-ack-broadcast");
        await channel.Subscribe();
        var sendTask = channel.Send(Constants.ChannelEventName.Broadcast, "test_event",
            new BroadcastExample { UserId = Guid.NewGuid().ToString() });
        var winner = await Task.WhenAny(sendTask, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(sendTask, winner, "channel.Send() did not complete within 5s (issue #38).");
        Assert.IsTrue(await sendTask);
    }

    [TestMethod]
    public async Task Broadcast_ShouldReplayHistory_GivenPrivateChannel()
    {
        // Unique per run: this test seeds history on the topic via RPC, then expects to replay it
        // back — the shared literal "online-users" would also replay stray "user" events left
        // behind by other tests/runs on the same topic, on top of the cross-test collision risk
        // described in PresenceTrackingTests.
        var topic = $"online-users-{Guid.NewGuid()}";
        var send = new Dictionary<string, object>
        {
            { "event", "user" },
            { "topic", topic },
            { "private", true }
        };
        await this.restClient.Rpc("send", send);
        var tsc = new TaskCompletionSource<bool>();
        var client1 = Helpers.PrivateSocketClient();
        await client1.ConnectAsync();
        var broadcastOptions = new BroadcastOptions
        {
            BroadcastAck = true,
            BroadcastSelf = true,
            Replay = new BroadcastOptions.ReplayOptions
            {
                Limit = 10,
                Since = DateTimeOffset.UtcNow.AddDays(-3).ToUnixTimeMilliseconds()
            }
        };
        var channel1 = client1.Channel(topic,
            ChannelOptions.Private(client1.Options, () => null, Wire.Settings()));
        var broadcast1 = channel1.Register<BroadcastExample>(broadcastOptions);
        broadcast1.AddBroadcastEventHandler((_, _) =>
        {
            var broadcast = broadcast1.Current();
            if (broadcast is { Event: "user", Meta.Replayed: true })
                tsc.TrySetResult(true);
        });
        await channel1.Subscribe();
        await tsc.Task;
    }
}

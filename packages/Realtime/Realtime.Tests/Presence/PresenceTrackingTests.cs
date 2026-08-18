using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Models;
using Supabase.Postgrest.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;

namespace Realtime.Tests.Presence;

/// <summary>
///     Presence against the live stack: two clients tracking presence on the same channel each observe the
///     other's state through the sync event.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class PresenceTrackingTests
{
    private IPostgrestClient restClient = null!;
    private IRealtimeClient<RealtimeSocket, RealtimeChannel> socketClient = null!;

    [TestInitialize]
    public async Task InitializeTest()
    {
        restClient = Helpers.RestClient();
        socketClient = Helpers.SocketClient();
        await socketClient.ConnectAsync();
    }

    [TestCleanup]
    public void CleanupTest() => socketClient.Disconnect();

    [TestMethod]
    public async Task Track_ShouldSyncPresenceBetweenClients()
    {
        var tsc = new TaskCompletionSource<bool>();
        var tsc2 = new TaskCompletionSource<bool>();
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        // A topic unique to this test run, not the shared literal "online-users": that literal is
        // also used by BroadcastRelayTests, and Client.Disconnect() doesn't await the close
        // handshake (fire-and-forget), so a prior test's connection to the same topic can still be
        // live server-side when this test starts — cross-test presence/broadcast cross-talk on a
        // shared topic, which showed up as CI-only failures (slower round-trip widens the overlap
        // window) that never reproduced locally.
        var topic = $"online-users-{Guid.NewGuid()}";
        var channel1 = socketClient.Channel(topic);
        var presence1 = channel1.Register<PresenceExample>(guid1);
        presence1.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (_, _) =>
        {
            var state = presence1.CurrentState;
            if (state.ContainsKey(guid2) && state[guid2].First().Time != null)
                tsc.TrySetResult(true);
        });
        var client2 = Helpers.SocketClient();
        await client2.ConnectAsync();
        var channel2 = client2.Channel(topic);
        var presence2 = channel2.Register<PresenceExample>(guid2);
        presence2.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (_, _) =>
        {
            var state = presence2.CurrentState;
            if (state.ContainsKey(guid1) && state[guid1].First().Time != null)
                tsc2.TrySetResult(true);
        });
        await channel1.Subscribe();
        await channel2.Subscribe();
        await presence1.Track(new PresenceExample { Time = DateTime.Now });
        await presence2.Track(new PresenceExample { Time = DateTime.Now });

        // Both peers must observe each other's presence before either untracks. Untracking
        // immediately (the previous behavior here) raced the join sync event against the
        // peer's confirmation: on a fast, low-latency local stack the confirmation usually
        // won, but under CI's slower/noisier network to the live stack, delivery order could
        // flip and the confirmation would never arrive — a genuine test race, not gate flakiness.
        var bothSynced = Task.WhenAll(tsc.Task, tsc2.Task);
        var completed = await Task.WhenAny(bothSynced, Task.Delay(15000));
        Assert.AreSame(bothSynced, completed, "Timed out waiting for both clients to observe each other's presence.");

        await presence1.Untrack();
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;

namespace RealtimeTests;

public class PresenceExample : BasePresence
{
    [JsonProperty("time")] public DateTime? Time { get; set; }
}

[TestClass]
public class ChannelPresenceTests
{
    private IPostgrestClient? _restClient;
    private IRealtimeClient<RealtimeSocket, RealtimeChannel>? _socketClient;

    [TestInitialize]
    public async Task InitializeTest()
    {
        _restClient = Helpers.RestClient();
        _socketClient = Helpers.SocketClient();
        await _socketClient!.ConnectAsync();
    }

    [TestCleanup]
    public void CleanupTest()
    {
        _socketClient!.Disconnect();
    }

    [TestMethod("Channel: Can create presence")]
    public async Task ClientCanCreatePresence()
    {
        var tsc = new TaskCompletionSource<bool>();
        var tsc2 = new TaskCompletionSource<bool>();

        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();

        var channel1 = _socketClient!.Channel("online-users");
        var presence1 = channel1.Register<PresenceExample>(guid1);
        presence1.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (_, _) =>
        {
            var state = presence1.CurrentState;
            if (state.ContainsKey(guid2) && state[guid2].First().Time != null)
                tsc.TrySetResult(true);
        });

        var client2 = Helpers.SocketClient();
        await client2.ConnectAsync();
        var channel2 = client2.Channel("online-users");
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

        await presence1.Untrack();
        
        await Task.WhenAll(new[] { tsc.Task, tsc2.Task });
    }
}
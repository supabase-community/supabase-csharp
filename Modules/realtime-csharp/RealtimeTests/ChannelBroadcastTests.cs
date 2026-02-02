using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest.Interfaces;
using RealtimeTests.Models;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace RealtimeTests;

public class BroadcastExample : BaseBroadcast
{
    [JsonProperty("userId")] public string? UserId { get; set; }
}

[TestClass]
public class ChannelBroadcastTests
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

    [TestMethod("Channel: Can listen for broadcast")]
    public async Task ClientCanListenForBroadcast()
    {
        var tsc = new TaskCompletionSource<bool>();
        var tsc2 = new TaskCompletionSource<bool>();

        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();

        var channel1 = _socketClient!.Channel("online-users");
        var broadcast1 = channel1.Register<BroadcastExample>(true, true);
        broadcast1.AddBroadcastEventHandler((_, _) =>
        {
            var broadcast = broadcast1.Current();
            if (broadcast?.UserId != guid1 && broadcast?.Event == "user")
                tsc.TrySetResult(true);
        });

        var client2 = Helpers.SocketClient();
        await client2.ConnectAsync();
        var channel2 = client2.Channel("online-users");
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

        await Task.WhenAll(new[] { tsc.Task, tsc2.Task });
    }

    [TestMethod("Channel: Payload returns a modeled response (if possible)")]
    public async Task ChannelPayloadReturnsModel()
    {
        var tsc = new TaskCompletionSource<bool>();

        var channel = _socketClient!.Channel("example");
        channel.Register(new PostgresChangesOptions("public", "*"));
        channel.AddPostgresChangeHandler(ListenType.Inserts, (_, changes) =>
        {
            var model = changes.Model<Todo>();
            tsc.SetResult(model != null);
        });

        await channel.Subscribe();

        await _restClient!.Table<Todo>().Insert(new Todo { UserId = 1, Details = "Client Models a response? ✅" });

        var check = await tsc.Task;
        Assert.IsTrue(check);
    }
}
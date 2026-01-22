using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest.Interfaces;
using RealtimeTests.Models;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.Constants;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace RealtimeTests;

[TestClass]
public class ChannelTests
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

    [TestMethod("Channel: Close Event Handler")]
    public async Task ChannelCloseEventHandler()
    {
        var tsc = new TaskCompletionSource<bool>();

        var channel = _socketClient!.Channel("realtime", "public", "todos");
        channel.AddStateChangedHandler((_, state) =>
        {
            if (state == ChannelState.Closed)
                tsc.SetResult(true);
        });

        await channel.Subscribe();
        channel.Unsubscribe();

        var check = await tsc.Task;
        Assert.IsTrue(check);
    }

    [TestMethod("Channel: Supports WALRUS Array Changes")]
    public async Task ChannelSupportsWalrusArray()
    {
        Todo? result = null;
        var tsc = new TaskCompletionSource<bool>();

        var channel = _socketClient!.Channel("realtime", "public", "todos");
        var numbers = new List<int> { 4, 5, 6 };

        await channel.Subscribe();

        channel.AddPostgresChangeHandler(ListenType.Inserts, (_, changes) =>
        {
            result = changes.Model<Todo>();
            tsc.SetResult(true);
        });

        await _restClient!.Table<Todo>().Insert(new Todo { UserId = 1, Numbers = numbers });

        await tsc.Task;
        CollectionAssert.AreEqual(numbers, result?.Numbers);
    }

    [TestMethod("Channel: Sends Join parameters")]
    public async Task ChannelSendsJoinParameters()
    {
        var parameters = new Dictionary<string, string> { { "key", "value" } };
        var channel = _socketClient!.Channel("realtime", "public", "todos", parameters: parameters);

        await channel.Subscribe();

        var serialized = JsonConvert.SerializeObject(channel.JoinPush?.Payload);
        Assert.IsTrue(serialized.Contains("\"key\":\"value\""));
    }

    [TestMethod("Channel: Returns single subscription per unique topic.")]
    public async Task ChannelJoinsDuplicateSubscription()
    {
        var subscription1 = _socketClient!.Channel("realtime", "public", "todos");
        var subscription2 = _socketClient!.Channel("realtime", "public", "todos");
        var subscription3 = _socketClient!.Channel("realtime", "public", "todos", "user_id", "1");

        Assert.AreEqual(subscription1.Topic, subscription2.Topic);

        await subscription1.Subscribe();

        Assert.AreEqual(subscription1.HasJoinedOnce, subscription2.HasJoinedOnce);
        Assert.AreNotEqual(subscription1.HasJoinedOnce, subscription3.HasJoinedOnce);

        var subscription4 = _socketClient!.Channel("realtime", "public", "todos");

        Assert.AreEqual(subscription1.HasJoinedOnce, subscription4.HasJoinedOnce);
    }

    [TestMethod("Channel: Registers Handlers")]
    public async Task ChannelRegistersHandlers()
    {
        var channel = _socketClient!.Channel("test");

        IRealtimeChannel.StateChangedHandler stateHandler = (_, _) => Assert.Fail("State Handler was called");
        IRealtimeChannel.MessageReceivedHandler messageReceivedHandler =
            (_, _) => Assert.Fail("Message Handler was called");
        IRealtimeChannel.PostgresChangesHandler postgresChangesHandler =
            (_, _) => Assert.Fail("Postgres Changes Handler was called");

        channel.AddStateChangedHandler(stateHandler);
        channel.AddMessageReceivedHandler(messageReceivedHandler);
        channel.AddPostgresChangeHandler(ListenType.All, postgresChangesHandler);

        channel.Register(new PostgresChangesOptions("public", "todos"));
        channel.Register<BroadcastExample>();
        channel.Register<PresenceExample>("user");

        channel.RemoveStateChangedHandler(stateHandler);
        channel.RemoveMessageReceivedHandler(messageReceivedHandler);
        channel.RemovePostgresChangeHandler(ListenType.All, postgresChangesHandler);

        await channel.Subscribe();

        await Task.Delay(500);
    }
}
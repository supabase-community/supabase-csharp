using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime;
using Supabase.Realtime.Exceptions;
using static Supabase.Realtime.Constants;

namespace Realtime.Tests.Channels;

/// <summary>
///     How the client resolves channels from a live connection: the topic each overload produces, that the
///     same topic returns one shared instance, that an already-prefixed name is not double-prefixed, removal,
///     and pushing the access token on <c>SetAuth</c>.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class ChannelSubscriptionTests
{
    private Client client = null!;

    [TestInitialize]
    public async Task InitializeTest()
    {
        client = Helpers.SocketClient();
        await client.ConnectAsync();
    }

    [TestCleanup]
    public void CleanupTest() => client.Disconnect();

    [TestMethod]
    public async Task Channel_ShouldTopicByTable()
    {
        var channel = client.Channel(table: "todos");
        await channel.Subscribe();
        Assert.AreEqual("realtime:public:todos", channel.Topic);
    }

    [TestMethod]
    public async Task Channel_ShouldTopicBySchemaWildcard()
    {
        var channel = client.Channel("realtime", "public", "*");
        await channel.Subscribe();
        Assert.AreEqual("realtime:public:*", channel.Topic);
    }

    [TestMethod]
    public async Task Channel_ShouldThrowThenJoin_GivenUnpublishedThenPublishedTable()
    {
        var users = client.Channel("realtime", "public", "users");
        await Assert.ThrowsAsync<RealtimeException>(() => users.Subscribe());
        var todos = client.Channel("realtime", "public", "todos");
        await todos.Subscribe();
        Assert.AreEqual("realtime:public:todos", todos.Topic);
    }

    [TestMethod]
    public async Task Channel_ShouldTopicByColumnFilter()
    {
        var channel = client.Channel("realtime", "public", "todos", "id", "1");
        await channel.Subscribe();
        Assert.AreEqual("realtime:public:todos:id=eq.1", channel.Topic);
    }

    [TestMethod]
    public async Task Channel_ShouldReturnSameInstance_GivenSameTopic()
    {
        var first = client.Channel("realtime", "public", "todos");
        await first.Subscribe();
        var second = client.Channel("realtime", "public", "todos");
        Assert.AreEqual(true, second.IsJoined);
    }

    [TestMethod]
    public void Channel_ShouldNotDoublePrefixTopic_GivenNameAlreadyPrefixed()
    {
        var prefixed = client.Channel("realtime:public:todos");
        Assert.AreEqual("realtime:public:todos", prefixed.Topic);
        Assert.AreSame(prefixed, client.Channel("public:todos"));
    }

    [TestMethod]
    public async Task Remove_ShouldClearStoredSubscription()
    {
        var channel = client.Channel("realtime", "public", "todos");
        await channel.Subscribe();
        client.Remove(channel);
        var reopened = client.Channel("realtime", "public", "todos");
        Assert.AreEqual(ChannelState.Closed, reopened.State);
    }

    [TestMethod]
    public async Task SetAuth_ShouldPushAccessTokenToJoinedChannelsOnly()
    {
        var first = client.Channel("realtime", "public", "todos");
        var second = client.Channel("realtime", "public", "todos");
        const string token =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";
        client.SetAuth(token);
        foreach (var subscription in client.Subscriptions.Values)
            Assert.IsNull(subscription.LastPush, "SetAuth stores the token but must not push it to a channel that has not joined yet");
        await first.Subscribe();
        await second.Subscribe();
        client.SetAuth(token);
        foreach (var subscription in client.Subscriptions.Values)
            Assert.IsTrue(subscription.LastPush?.EventName == ChannelAccessToken, "once joined, SetAuth pushes the refreshed access_token to the channel");
    }
}

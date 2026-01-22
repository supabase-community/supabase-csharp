using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime.Exceptions;
using static Supabase.Realtime.Constants;

namespace RealtimeTests;

[TestClass]
public class ClientTests
{
    private Supabase.Realtime.Client? client;

    [TestInitialize]
    public async Task InitializeTest()
    {
        Console.WriteLine();
        Console.WriteLine(Dns.GetHostEntryAsync(Dns.GetHostName()).GetAwaiter().GetResult().AddressList[0]);

        client = Helpers.SocketClient();
        client.AddDebugHandler((sender, message, exception) => Debug.WriteLine(message));

        await client!.ConnectAsync();
    }

    [TestCleanup]
    public void CleanupTest()
    {
        client?.Disconnect();
    }


    [TestMethod("Client: Join channels of format: {database}")]
    public async Task ClientJoinsChannel_DB()
    {
        var channel = client!.Channel(table: "todos");
        await channel.Subscribe();

        Assert.AreEqual("realtime:public:todos", channel.Topic);
    }

    [TestMethod("Client: Join channels of format: {database}:{schema}:*")]
    public async Task ClientJoinsChannel_DB_Schema()
    {
        var channel = client!.Channel("realtime", "public", "*");
        await channel.Subscribe();

        Assert.AreEqual("realtime:public:*", channel.Topic);
    }

    [TestMethod("Client: Join channels of format: {database}:{schema}:{table}")]
    public async Task ClientJoinsChannel_DB_Schema_Table()
    {
        var channel = client!.Channel("realtime", "public", "users");
        await Assert.ThrowsExceptionAsync<RealtimeException>(() => channel.Subscribe());

        var channel2 = client!.Channel("realtime", "public", "todos");
        await channel2.Subscribe();

        Assert.AreEqual("realtime:public:todos", channel2.Topic);
    }

    [TestMethod("Client: Join channels of format: {database}:{schema}:{table}:{col}=eq.{val}")]
    public async Task ClientJoinsChannel_DB_Schema_Table_Query()
    {
        var channel = client!.Channel("realtime", "public", "todos", "id", "1");
        await channel.Subscribe();

        Assert.AreEqual("realtime:public:todos:id=eq.1", channel.Topic);
    }

    [TestMethod("Client: Returns a single instance of a channel based on topic")]
    public async Task ClientReturnsSingleChannelInstance()
    {
        var channel1 = client!.Channel("realtime", "public", "todos");

        await channel1.Subscribe();

        // Client should return an instance of `realtime:public:todos` that is already joined.
        var channel2 = client!.Channel("realtime", "public", "todos");

        Assert.AreEqual(true, channel2.IsJoined);
    }

    [TestMethod("Client: Removes Channel Subscriptions")]
    public async Task ClientCanRemoveChannelSubscription()
    {
        var channel1 = client!.Channel("realtime", "public", "todos");
        await channel1.Subscribe();

        // Removing channel should remove the stored instance, so a future instance would need
        // to resubscribe.
        client!.Remove(channel1);

        var channel2 = client!.Channel("realtime", "public", "todos");
        Assert.AreEqual(ChannelState.Closed, channel2.State);
    }

    [TestMethod("Client: SetsAuth")]
    public async Task ClientSetsAuth()
    {
        var channel = client!.Channel("realtime", "public", "todos");
        var channel2 = client!.Channel("realtime", "public", "todos");

        var token =
            @"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.C8oVtF5DICct_4HcdSKt8pdrxBFMQOAnPpbiiUbaXAY";

        // No subscriptions should show a push
        client!.SetAuth(token);
        foreach (var subscription in client!.Subscriptions.Values)
        {
            Assert.IsNull(subscription.LastPush);
        }

        await channel.Subscribe();
        await channel2.Subscribe();

        client!.SetAuth(token);
        foreach (var subscription in client!.Subscriptions.Values)
        {
            Assert.IsTrue(subscription?.LastPush?.EventName == ChannelAccessToken);
        }
    }

    [TestMethod("Client: Can reconnect after programmatic disconnect")]
    public async Task ClientCanReconnectAfterProgrammaticDisconnect()
    {
        client!.Disconnect();
        await client!.ConnectAsync();
    }

    [TestMethod("Client: Sets headers")]
    public async Task ClientCanSetHeaders()
    {
        client!.Disconnect();
        
        client!.GetHeaders = () => new Dictionary<string, string>() { { "testing", "123" } };
        await client.ConnectAsync();
        
        Assert.IsNotNull(client!);
        Assert.IsNotNull(client!.Socket);
        Assert.IsNotNull(client!.Socket.GetHeaders);
        Assert.AreEqual("123",client.Socket.GetHeaders()["testing"]);
    }
}
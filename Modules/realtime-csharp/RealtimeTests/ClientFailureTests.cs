using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime;
using Supabase.Realtime.Exceptions;

namespace RealtimeTests;

[TestClass]
public class ClientFailureTests
{
    [TestMethod("Client throws exception when unable to initially connect.")]
    public async Task ClientThrowsExceptionOnInitialConnectionFailure()
    {
        var client = new Client("ws://localhost");
        var client2 = new Client("ws://localhost");
        client.AddDebugHandler((_, message, _) => Debug.WriteLine($"Client 1: {message}"));
        client2.AddDebugHandler((_, message, _) => Debug.WriteLine($"Client 2: {message}"));

        await Assert.ThrowsExceptionAsync<RealtimeException>(async () => { await client.ConnectAsync(); });

        await Assert.ThrowsExceptionAsync<RealtimeException>(() =>
        {
            var tsc = new TaskCompletionSource();

            client2.Connect((_, exception) =>
            {
                if (exception != null)
                    tsc.SetException(exception);
            });
            return tsc.Task;
        });
    }

    [TestMethod("Client: Allows for multiple connection attempts after failure.")]
    public async Task ClientShouldAllowForMultipleSocketConnectionAttempts()
    {
        var client = new Client("ws://localhost");
        client.AddDebugHandler((_, message, _) => Debug.WriteLine(message));

        // Should throw first time and clear socket instance.
        await Assert.ThrowsExceptionAsync<RealtimeException>(client.ConnectAsync);
        
        // Should throw again, as socket is still cleared (as opposed to merely logging).
        await Assert.ThrowsExceptionAsync<RealtimeException>(client.ConnectAsync);
    }
}
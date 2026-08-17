using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;

namespace Postgrest.Tests.Events;

/// <summary>
///     The observable request lifecycle: a registered <see cref="OnRequestPreparedEventHandler" /> is invoked
///     as a request goes out, and a cancelled <see cref="CancellationToken" /> aborts the in-flight request.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class RequestLifecycleTests
{
    [TestMethod]
    public async Task RequestPreparedHandler_ShouldBeInvoked_GivenARequestIsSent()
    {
        var client = LocalStack.Client();
        var handler = Substitute.For<OnRequestPreparedEventHandler>();
        client.AddRequestPreparedHandler(handler);
        try
        {
            await client.Table<Movie>().Get();
            handler.Received().Invoke(Arg.Any<object>(), Arg.Any<ClientOptions>(), Arg.Any<HttpMethod>(),
                Arg.Any<string>(), Arg.Any<JsonSerializerOptions>(), Arg.Any<object?>(),
                Arg.Any<Dictionary<string, string>?>());
        }
        finally
        {
            client.ClearRequestPreparedHandlers();
        }
    }

    [TestMethod]
    public async Task Insert_ShouldAbort_GivenACancelledToken()
    {
        var client = LocalStack.Client();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = () => client.Table<KitchenSink>()
            .Insert(new KitchenSink { DateTimeValue = DateTime.UtcNow }, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

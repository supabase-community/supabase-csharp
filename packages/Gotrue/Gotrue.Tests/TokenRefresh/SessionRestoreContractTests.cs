#region

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

#endregion

namespace Gotrue.Tests.TokenRefresh;

/// <summary>
///     Pins how a restored session survives startup: only a server-rejected refresh token signs the
///     user out - a network error or an offline client keeps the persisted session (issue #390).
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class SessionRestoreContractTests
{
    private IGotrueSessionPersistence<Session> persistence = null!;
    private MockGotrueServer server = null!;

    [TestInitialize]
    public void TestInitializer()
    {
        this.server = new MockGotrueServer();
        this.persistence = SessionPersistenceSubstitute.Tracking();
        this.persistence.SaveSession(new Session { AccessToken = "an-access-token", RefreshToken = "a-refresh-token", ExpiresIn = 3600 });
    }

    [TestCleanup]
    public void TestCleanup() => this.server.Dispose();

    [TestMethod]
    public async Task RetrieveSessionAsync_ShouldKeepTheSession_GivenTheRefreshFailsWithANetworkError()
    {
        var client = this.Restore(TestClients.Against(this.server, autoRefreshToken: true, new HttpClient(new UnreachableHandler())));
        var session = await client.RetrieveSessionAsync();
        session.Should().NotBeNull("an offline start must not sign the user out");
        client.CurrentSession.Should().BeSameAs(session);
        this.persistence.DidNotReceive().DestroySession();
        client.Shutdown();
    }

    [TestMethod]
    public async Task RetrieveSessionAsync_ShouldDestroyTheSession_GivenTheServerRejectsTheRefreshToken()
    {
        this.server
            .Given(Request.Create().WithPath("/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Fixture("token_not_found_error.json")));
        var client = this.Restore(TestClients.Against(this.server, autoRefreshToken: true));
        var session = await client.RetrieveSessionAsync();
        session.Should().BeNull();
        client.CurrentSession.Should().BeNull();
        this.persistence.Received().DestroySession();
        client.Shutdown();
    }

    [TestMethod]
    public async Task RetrieveSessionAsync_ShouldKeepTheSession_GivenTheClientIsMarkedOffline()
    {
        var client = this.Restore(TestClients.Against(this.server, autoRefreshToken: true));
        client.Online = false;
        var session = await client.RetrieveSessionAsync();
        session.Should().NotBeNull("an offline client cannot refresh, but must not lose the session");
        client.CurrentSession.Should().BeSameAs(session);
        this.persistence.DidNotReceive().DestroySession();
        client.Shutdown();
    }

    private IGotrueClient<User, Session> Restore(IGotrueClient<User, Session> client)
    {
        client.SetPersistence(this.persistence);
        client.LoadSession();
        return client;
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TokenRefresh", "Fixtures", name));

    private sealed class UnreachableHandler : HttpMessageHandler
    {
        public int Attempts;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref this.Attempts);
            throw new HttpRequestException("connection refused");
        }
    }
}

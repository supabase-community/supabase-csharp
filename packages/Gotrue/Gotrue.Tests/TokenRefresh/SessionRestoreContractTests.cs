#region

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
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
        this.persistence.DidNotReceive().DestroySession();
        client.Shutdown();
    }

    [TestMethod]
    public void LoadSession_ShouldNotSignOut_GivenAnEmptyStore()
    {
        var empty = SessionPersistenceSubstitute.Tracking();
        var client = TestClients.Against(this.server);
        client.SetPersistence(empty);
        client.LoadSession();
        client.CurrentSession.Should().BeNull();
        empty.DidNotReceive().DestroySession();
    }

    [TestMethod]
    public async Task TokenRefresh_ShouldBackOffBetweenAttempts_GivenARestoredExpiredSession()
    {
        var handler = new UnreachableHandler();
        var client = TestClients.Against(this.server, autoRefreshToken: true, new HttpClient(handler));
        this.persistence.SaveSession(new Session { AccessToken = "an-access-token", RefreshToken = "a-refresh-token", ExpiresIn = 60, CreatedAt = DateTime.UtcNow.AddHours(-1) });
        this.Restore(client);
        await Task.Delay(1500);
        handler.Attempts.Should().BeGreaterThanOrEqualTo(1, "the first attempt for an expired session fires immediately");
        handler.Attempts.Should().BeLessThan(3, "a failed refresh must back off, not hot-loop");
        client.Shutdown();
    }

    [TestMethod]
    public async Task RefreshToken_ShouldShareOneAttempt_GivenConcurrentCallers()
    {
        var handler = new GatedHandler();
        var client = this.Restore(TestClients.Against(this.server, httpClient: new HttpClient(handler)));
        var first = client.RefreshToken();
        var second = client.RefreshToken();
        handler.Release();
        await Task.WhenAll(first, second);
        handler.Attempts.Should().Be(1, "a refresh token is single-use, so concurrent refreshes must share one attempt");
        await client.RefreshToken();
        handler.Attempts.Should().Be(2, "a completed attempt must not be reused");
    }

    [TestMethod]
    public async Task RefreshToken_ShouldDiscardTheResult_GivenTheSessionWasReplacedMidFlight()
    {
        var handler = new GatedHandler();
        var client = this.Restore(TestClients.Against(this.server, httpClient: new HttpClient(handler)));
        var refresh = client.RefreshToken();
        this.persistence.SaveSession(new Session { AccessToken = "another-access-token", RefreshToken = "another-refresh-token", ExpiresIn = 3600 });
        client.LoadSession();
        handler.Release();
        await refresh;
        client.CurrentSession!.RefreshToken.Should().Be("another-refresh-token",
            "the refresh belonged to the session that was replaced, so its result must not overwrite the new one");
    }

    [TestMethod]
    public async Task RefreshToken_ShouldStartItsOwnAttempt_GivenTheSessionWasReplaced()
    {
        var handler = new GatedHandler();
        var client = this.Restore(TestClients.Against(this.server, httpClient: new HttpClient(handler)));
        var stale = client.RefreshToken();
        this.persistence.SaveSession(new Session { AccessToken = "another-access-token", RefreshToken = "another-refresh-token", ExpiresIn = 3600 });
        client.LoadSession();
        var fresh = client.RefreshToken();
        handler.Release();
        await Task.WhenAll(stale, fresh);
        handler.Attempts.Should().Be(2,
            "the in-flight attempt holds the previous session's refresh token, so the new session must not join it");
    }

    [TestMethod]
    public async Task LoadSession_ShouldKeepTheUserSignedIn_GivenAClientWithoutPersistence()
    {
        this.server
            .Given(Request.Create().WithPath("/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Fixture("token_success.json")));
        var client = TestClients.Against(this.server);
        await client.SignIn("test@example.com", "a-password");
        client.LoadSession();
        client.CurrentSession.Should().NotBeNull("a client with no persistence has nothing to load, so LoadSession must leave it alone");
    }

    [TestMethod]
    public async Task RetrieveSessionAsync_ShouldReturnTheReplacement_GivenTheRejectedRefreshWasForAReplacedSession()
    {
        var handler = new GatedHandler(HttpStatusCode.BadRequest, "token_not_found_error.json");
        var client = this.Restore(TestClients.Against(this.server, autoRefreshToken: true, new HttpClient(handler)));
        var retrieve = client.RetrieveSessionAsync();
        this.persistence.SaveSession(new Session { AccessToken = "another-access-token", RefreshToken = "another-refresh-token", ExpiresIn = 3600 });
        client.LoadSession();
        handler.Release();
        var session = await retrieve;
        session!.RefreshToken.Should().Be("another-refresh-token",
            "the rejection was for the replaced session, so the live one must be returned, not null");
        this.persistence.DidNotReceive().DestroySession();
    }

    [TestMethod]
    public void LoadSession_ShouldClearTheSession_GivenTheStoreWasEmptied()
    {
        var client = this.Restore(TestClients.Against(this.server));
        this.persistence.DestroySession();
        client.LoadSession();
        client.CurrentSession.Should().BeNull("a store emptied behind the client's back must not leave a stale session in memory");
    }

    [TestMethod]
    public async Task RefreshToken_ShouldRetryAfterAFailedAttempt_GivenSequentialCallers()
    {
        var handler = new UnreachableHandler();
        var client = this.Restore(TestClients.Against(this.server, httpClient: new HttpClient(handler)));
        for (var i = 0; i < 2; i++)
        {
            var act = () => client.RefreshToken();
            await act.Should().ThrowAsync<Exception>();
        }
        handler.Attempts.Should().Be(2, "a failed attempt must not be cached as the in-flight refresh");
    }

    [TestMethod]
    public void LoadSession_ShouldNotThrow_GivenAStoreThatFailsToLoad()
    {
        var store = Substitute.For<IGotrueSessionPersistence<Session>>();
        store.LoadSession().Returns(_ => throw new IOException("locked"));
        var client = TestClients.Against(this.server);
        client.SetPersistence(store);
        var act = () => client.LoadSession();
        act.Should().NotThrow();
        client.CurrentSession.Should().BeNull();
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

    /// <summary>
    ///     Answers every request with a successful refresh, but not before the test opens the gate - so a
    ///     refresh can be held in flight while the test does something to the session behind its back.
    /// </summary>
    private sealed class GatedHandler(HttpStatusCode status = HttpStatusCode.OK, string fixture = "token_success.json") : HttpMessageHandler
    {
        private readonly TaskCompletionSource<bool> gate = new();

        public int Attempts;

        public void Release() => this.gate.TrySetResult(true);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref this.Attempts);
            await this.gate.Task;
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(Fixture(fixture), Encoding.UTF8, "application/json"),
            };
        }
    }
}

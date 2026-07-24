using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Supabase;
using Supabase.Functions.Interfaces;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Postgrest.Interfaces;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Storage;
using Supabase.Storage.Interfaces;
using static Supabase.Gotrue.Constants;

namespace SupabaseTests;

/// <summary>
/// The umbrella <see cref="Supabase.Client"/> owns no transport of its own — every request belongs to a
/// child package with its own tests. What it uniquely owns, and what this fixture pins, is how it
/// <em>composes</em> those children: constructing them, wiring Realtime to Postgrest, propagating the
/// auth headers, and forwarding auth-state changes. All hermetic; live round-trips are in
/// <see cref="SupabaseClientDatabaseTests"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SupabaseClientCompositionTests
{
    private static Supabase.Client UrlClient(SupabaseOptions options = null) =>
        new("http://localhost", "test-key", options ?? new SupabaseOptions { AutoConnectRealtime = false });

    // Builds the umbrella through its DI constructor with substituted children; a test overrides only
    // the collaborator it cares about.
    private static Supabase.Client DiClient(
        IGotrueClient<User, Session> auth = null,
        IRealtimeClient<RealtimeSocket, RealtimeChannel> realtime = null,
        IPostgrestClient postgrest = null) =>
        new(auth ?? Substitute.For<IGotrueClient<User, Session>>(),
            realtime ?? RealtimeSubstitute(),
            Substitute.For<IFunctionsClient>(),
            postgrest ?? Substitute.For<IPostgrestClient>(),
            Substitute.For<IStorageClient<Bucket, FileObject>>(),
            new SupabaseOptions());

    // The umbrella writes Options.PostgrestClient during construction, so the substitute needs real Options.
    private static IRealtimeClient<RealtimeSocket, RealtimeChannel> RealtimeSubstitute()
    {
        var realtime = Substitute.For<IRealtimeClient<RealtimeSocket, RealtimeChannel>>();
        realtime.Options.Returns(new Supabase.Realtime.ClientOptions());
        return realtime;
    }

    [TestMethod]
    public void SupabaseClient_ShouldComposeAllChildClients_GivenUrlConstructor()
    {
        var client = UrlClient();
        using (new AssertionScope())
        {
            client.Auth.Should().NotBeNull();
            client.Realtime.Should().NotBeNull();
            client.Functions.Should().NotBeNull();
            client.Postgrest.Should().NotBeNull();
            client.Storage.Should().NotBeNull();
        }
    }

    [TestMethod]
    public void SupabaseClient_ShouldWireRealtimeToOwnPostgrestClient_GivenUrlConstructor()
    {
        var client = UrlClient();
        client.Realtime.Options.PostgrestClient.Should().BeSameAs(client.Postgrest,
            "postgres_changes models must reach the same Postgrest client the umbrella exposes (issue #35)");
    }

    [TestMethod]
    public void SupabaseClient_ShouldWireRealtimeToInjectedPostgrestClient_GivenDependencyInjectionConstructor()
    {
        var postgrest = Substitute.For<IPostgrestClient>();
        var realtime = RealtimeSubstitute();
        _ = DiClient(realtime: realtime, postgrest: postgrest);
        realtime.Options.PostgrestClient.Should().BeSameAs(postgrest);
    }

    [TestMethod]
    public void SupabaseClient_ShouldExposeInjectedChildren_GivenChildrenSetViaProperties()
    {
        var client = UrlClient();
        var auth = Substitute.For<IGotrueClient<User, Session>>();
        var functions = Substitute.For<IFunctionsClient>();
        var realtime = Substitute.For<IRealtimeClient<RealtimeSocket, RealtimeChannel>>();
        var postgrest = Substitute.For<IPostgrestClient>();
        var storage = Substitute.For<IStorageClient<Bucket, FileObject>>();
        client.Auth = auth;
        client.Functions = functions;
        client.Realtime = realtime;
        client.Postgrest = postgrest;
        client.Storage = storage;
        using (new AssertionScope())
        {
            client.Auth.Should().BeSameAs(auth);
            client.Functions.Should().BeSameAs(functions);
            client.Realtime.Should().BeSameAs(realtime);
            client.Postgrest.Should().BeSameAs(postgrest);
            client.Storage.Should().BeSameAs(storage);
        }
    }

    [TestMethod]
    public void SupabaseClient_ShouldComposeApiKeyBearerAndClientInfoHeaders()
    {
        var headers = UrlClient().Postgrest.GetHeaders!();
        using (new AssertionScope())
        {
            headers.Should().ContainKey("apiKey").WhoseValue.Should().Be("test-key");
            headers.Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer test-key",
                "with no session the bearer falls back to the supabase key");
            headers.Should().ContainKey("X-Client-Info");
        }
    }

    [TestMethod]
    public void SupabaseClient_ShouldPreferDeveloperAuthorizationHeader_GivenAuthorizationInOptions()
    {
        var options = new SupabaseOptions
        {
            AutoConnectRealtime = false,
            Headers =
            {
                ["Authorization"] = "Bearer developer-token"
            }
        };
        UrlClient(options).Postgrest.GetHeaders!().Should().ContainKey("Authorization")
            .WhoseValue.Should().Be("Bearer developer-token",
                "an explicit Authorization header must win over the key-derived bearer (issue #5)");
    }

    [TestMethod]
    [DataRow(AuthState.SignedIn)]
    [DataRow(AuthState.TokenRefreshed)]
    [DataRow(AuthState.UserUpdated)]
    public void SupabaseClient_ShouldForwardAccessTokenToRealtime_GivenAuthStateCarriesSession(AuthState state)
    {
        var realtime = RealtimeSubstitute();
        var auth = Substitute.For<IGotrueClient<User, Session>>();
        auth.CurrentSession.Returns(new Session { AccessToken = "signed-in-token" });
        var client = DiClient(realtime: realtime);
        IGotrueClient<User, Session>.AuthEventHandler captured = null;
        auth.When(a => a.AddStateChangedListener(Arg.Any<IGotrueClient<User, Session>.AuthEventHandler>()))
            .Do(call => captured = call.Arg<IGotrueClient<User, Session>.AuthEventHandler>());
        client.Auth = auth;
        captured!.Invoke(auth, state);
        realtime.Received().SetAuth("signed-in-token");
    }

    [TestMethod]
    [DataRow(AuthState.SignedOut)]
    [DataRow(AuthState.PasswordRecovery)]
    [DataRow(AuthState.Shutdown)]
    public void SupabaseClient_ShouldNotForwardTokenToRealtime_GivenAuthStateCarriesNoSession(AuthState state)
    {
        var realtime = RealtimeSubstitute();
        realtime.Subscriptions.Returns(new ReadOnlyDictionary<string, RealtimeChannel>(
            new Dictionary<string, RealtimeChannel>()));
        var auth = Substitute.For<IGotrueClient<User, Session>>();
        var client = DiClient(realtime: realtime);
        IGotrueClient<User, Session>.AuthEventHandler captured = null;
        auth.When(a => a.AddStateChangedListener(Arg.Any<IGotrueClient<User, Session>.AuthEventHandler>()))
            .Do(call => captured = call.Arg<IGotrueClient<User, Session>.AuthEventHandler>());
        client.Auth = auth;
        captured!.Invoke(auth, state);
        realtime.DidNotReceive().SetAuth(Arg.Any<string>());
    }

    [TestMethod]
    public void SupabaseClient_ShouldDelegateRpcToPostgrest()
    {
        var postgrest = Substitute.For<IPostgrestClient>();
        _ = DiClient(postgrest: postgrest).Rpc("my_function", null);
        postgrest.Received().Rpc("my_function", null);
    }

    [TestMethod]
    public void SupabaseClient_ShouldReturnQueryableTable_GivenFrom()
    {
        UrlClient().From<Models.Channel>().Should().NotBeNull();
    }

    [TestMethod]
    public void SupabaseClient_ShouldDeriveFunctionsUrl_GivenHostedSupabaseUrl()
    {
        var client = new Supabase.Client("https://abcdefgh.supabase.co", "test-key",
            new SupabaseOptions { AutoConnectRealtime = false });
        client.Functions.Should().NotBeNull();
    }

    [TestMethod]
    public void SupabaseClient_ShouldExposeAdminAuthClient_GivenServiceKey()
    {
        UrlClient().AdminAuth("service-key").Should().NotBeNull();
    }

    [TestMethod]
    public void SupabaseClient_ShouldPreferSessionTokenOverApiKey_GivenActiveSession()
    {
        var client = UrlClient();
        var auth = Substitute.For<IGotrueClient<User, Session>>();
        auth.CurrentSession.Returns(new Session { AccessToken = "session-token" });
        client.Auth = auth;
        client.Postgrest.GetHeaders!().Should().ContainKey("Authorization")
            .WhoseValue.Should().Be("Bearer session-token",
                "an active session's access token must take precedence over the api key as the bearer");
    }

    [TestMethod]
    public void SupabaseClient_ShouldStopListeningToPreviousAuth_GivenAuthReplaced()
    {
        var previous = Substitute.For<IGotrueClient<User, Session>>();
        var client = DiClient(auth: previous);
        client.Auth = Substitute.For<IGotrueClient<User, Session>>();
        previous.Received().RemoveStateChangedListener(Arg.Any<IGotrueClient<User, Session>.AuthEventHandler>());
    }

    [TestMethod]
    public void SupabaseClient_ShouldDisconnectPreviousRealtime_GivenRealtimeReplaced()
    {
        var previous = RealtimeSubstitute();
        var client = DiClient(realtime: previous);
        client.Realtime = Substitute.For<IRealtimeClient<RealtimeSocket, RealtimeChannel>>();
        previous.Received().Disconnect(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>());
    }
}
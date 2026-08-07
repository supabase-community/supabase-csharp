#region

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the guards around linking with an ID token: only the native OIDC providers are accepted, and a
///     caller must already be signed in — both fail fast without touching the network.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class LinkIdentityWithIdTokenTests
{
    private MockGotrueServer server = null!;

    [TestInitialize]
    public void TestInitializer() => server = new MockGotrueServer();

    [TestCleanup]
    public void TestCleanup() => server.Dispose();

    [TestMethod]
    public async Task LinkIdentityWithIdToken_ShouldThrow_GivenUnsupportedProvider()
    {
        var api = new Api(server.Url);
        var link = () => api.LinkIdentityWithIdToken("user-access-token", new LinkIdentityWithIdTokenOptions(Constants.Provider.Github, "id-token"));
        await link.Should().ThrowAsync<GotrueException>("id_token linking is only defined for the native OIDC providers");
    }

    [TestMethod]
    public async Task LinkIdentityWithIdToken_ShouldThrow_GivenNoSignedInUser()
    {
        var client = TestClients.Against(server);
        var link = () => client.LinkIdentityWithIdToken(new LinkIdentityWithIdTokenOptions(Constants.Provider.Google, "id-token"));
        await link.Should().ThrowAsync<GotrueException>("linking an identity requires an authenticated user");
    }
}

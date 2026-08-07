#region

using System.Net.Http;
using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the request UnlinkIdentity puts on the wire: the identity id must land in the DELETE path
///     verbatim. A literal '$' had leaked into the URL template ($"{Url}/user/identities/${IdentityId}"),
///     so the SDK deleted at "/user/identities/$&lt;id&gt;" and the call never hit the real identity.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class UnlinkIdentityContractTests
{
    private IGotrueApi<User, Session> api = null!;
    private MockGotrueServer server = null!;

    [TestInitialize]
    public void TestInitializer()
    {
        server = new MockGotrueServer();
        api = new Api(server.Url);
    }

    [TestCleanup]
    public void TestCleanup() => server.Dispose();

    [TestMethod]
    public async Task UnlinkIdentity_ShouldDeleteAtTheIdentityPath_GivenUserIdentity()
    {
        server.Given(Request.Create().UsingDelete()).RespondWith(Response.Create().WithStatusCode(200));
        await api.UnlinkIdentity("access-token", new UserIdentity { IdentityId = "identity-123" });
        server.VerifySingleReceivedRequest()
            .WithMethod(HttpMethod.Delete)
            .WithPath("/user/identities/identity-123");
    }
}

#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the exact bytes LinkIdentityWithIdToken puts on the wire: the provider enum mapping and the
///     <c>link_identity</c> boolean that distinguishes it from a plain ID-token sign-in. This is the
///     transport contract the System.Text.Json migration must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class IdentityLinkApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task LinkIdentityRequest_ShouldSerializeToExpectedPayload_GivenProviderAndIdToken()
    {
        await this.Api.LinkIdentityWithIdToken("user-jwt",
            new LinkIdentityWithIdTokenOptions(Constants.Provider.Google, "id-token-value"));
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    /// <summary>
    ///     Regression (#378): the provider-link GET must ask the server to skip its redirect so the
    ///     browser is not sent to the provider without the Authorization header ("No API key found
    ///     in request"). auth-js' linkIdentity sets skip_http_redirect for exactly this reason.
    /// </summary>
    [TestMethod]
    public async Task LinkIdentityRequest_ShouldCarrySkipHttpRedirect_GivenAnyProvider()
    {
        await this.Api.LinkIdentity("user-jwt", Constants.Provider.GitHub, new SignInOptions());
        this.EmittedRequest
            .WithPath("/user/identities/authorize")
            .WithQueryParam("skip_http_redirect", "true");
    }
}

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
}

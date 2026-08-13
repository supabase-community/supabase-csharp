#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Gotrue.Tests.TokenRefresh;

/// <summary>
///     Pins the exact bytes the token-refresh request puts on the wire (<c>refresh_token</c>). This is the
///     transport contract the System.Text.Json migration must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class RefreshApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task RefreshRequest_ShouldSerializeToExpectedPayload_GivenRefreshToken()
    {
        await this.Api.RefreshAccessToken("access-token-value", "refresh-token-value");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

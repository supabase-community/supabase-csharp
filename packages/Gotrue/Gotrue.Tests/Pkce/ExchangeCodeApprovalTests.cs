#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Gotrue.Tests.Pkce;

/// <summary>
///     Pins the exact bytes the PKCE code exchange puts on the wire (<c>auth_code</c> + <c>code_verifier</c>).
///     This is the transport contract the System.Text.Json migration must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class ExchangeCodeApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task ExchangeCodeRequest_ShouldSerializeToExpectedPayload_GivenVerifierAndAuthCode()
    {
        await this.Api.ExchangeCodeForSession("code-verifier-value", "auth-code-value");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the exact bytes the password-recovery request puts on the wire. This is the transport contract
///     the System.Text.Json migration must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class PasswordRecoveryApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task ResetPasswordRequest_ShouldSerializeToExpectedPayload_GivenEmail()
    {
        await this.Api.ResetPasswordForEmail("user@example.com");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

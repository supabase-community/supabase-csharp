#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue.Mfa;

#endregion

namespace Gotrue.Tests.Mfa;

/// <summary>
///     Pins the exact bytes the MFA enroll and verify requests put on the wire. This is the transport
///     contract the System.Text.Json migration must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class MultiFactorApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task EnrollRequest_ShouldSerializeToExpectedPayload_GivenTotpFactor()
    {
        await this.Api.Enroll("user-jwt",
            new MfaEnrollParams { FactorType = "totp", FriendlyName = "My Phone", Issuer = "example.com" });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task VerifyRequest_ShouldSerializeToExpectedPayload_GivenCodeAndChallenge()
    {
        await this.Api.Verify("user-jwt",
            new MfaVerifyParams { FactorId = "factor-id", ChallengeId = "challenge-id", Code = "123456" });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

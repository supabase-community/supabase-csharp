#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the exact bytes the OTP verification requests put on the wire. Each carries a <c>type</c> enum
///     mapped to its wire string (e.g. <c>magiclink</c>, <c>sms</c>) — a mapping the System.Text.Json
///     migration must reproduce byte for byte.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class OtpVerificationApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task VerifyOtpRequest_ShouldSerializeToExpectedPayload_GivenEmailToken()
    {
        await this.Api.VerifyEmailOTP("user@example.com", "123456", Constants.EmailOtpType.MagicLink);
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task VerifyOtpRequest_ShouldSerializeToExpectedPayload_GivenMobileToken()
    {
        await this.Api.VerifyMobileOTP("+15555550123", "123456", Constants.MobileOtpType.SMS);
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task VerifyOtpRequest_ShouldSerializeToExpectedPayload_GivenTokenHash()
    {
        await this.Api.VerifyTokenHash("token-hash-value", Constants.EmailOtpType.Recovery);
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

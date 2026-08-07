#region

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Mfa;

#endregion

namespace Gotrue.Tests.Mfa;

/// <summary>
///     End-to-end MFA rejections against the live stack: bad TOTP codes, an unknown factor type, and empty
///     factor/challenge identifiers each fail with a <see cref="GotrueException" />.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class MfaFailureTests : MfaFixture
{
    [TestMethod]
    public async Task ChallengeAndVerify_ShouldThrow_GivenWrongTotpCode()
    {
        await this.SignUpNewUser();
        var enrollment = await this.EnrollTotp();
        var verify = () => this.Client.ChallengeAndVerify(new MfaChallengeAndVerifyParams { FactorId = enrollment.Id, Code = "12345" });
        await verify.Should().ThrowAsync<GotrueException>();
    }

    [TestMethod]
    public async Task Enroll_ShouldThrow_GivenInvalidFactorType()
    {
        await this.SignUpNewUser();
        var enroll = () => this.Client.Enroll(new MfaEnrollParams { Issuer = "Supabase", FactorType = "InvalidType", FriendlyName = "Enroll test" });
        await enroll.Should().ThrowAsync<GotrueException>();
    }

    [TestMethod]
    public async Task Unenroll_ShouldThrow_GivenEmptyFactorId()
    {
        await this.SignUpNewUser();
        var unenroll = () => this.Client.Unenroll(new MfaUnenrollParams { FactorId = "" });
        await unenroll.Should().ThrowAsync<GotrueException>();
    }

    [TestMethod]
    public async Task Challenge_ShouldThrow_GivenEmptyFactorId()
    {
        await this.SignUpNewUser();
        var challenge = () => this.Client.Challenge(new MfaChallengeParams { FactorId = "" });
        await challenge.Should().ThrowAsync<GotrueException>();
    }

    [TestMethod]
    public async Task Verify_ShouldThrow_GivenEmptyChallengeId()
    {
        await this.SignUpNewUser();
        var enrollment = await this.EnrollTotp();
        var challenge = (await this.Client.Challenge(new MfaChallengeParams { FactorId = enrollment.Id }))!;
        challenge.Id.Should().NotBeNull();
        var verify = () => this.Client.Verify(new MfaVerifyParams { Code = "", ChallengeId = "", FactorId = enrollment.Id });
        await verify.Should().ThrowAsync<GotrueException>();
    }

    [TestMethod]
    public async Task Verify_ShouldThrow_GivenEmptyFactorId()
    {
        await this.SignUpNewUser();
        var enrollment = await this.EnrollTotp();
        var challenge = (await this.Client.Challenge(new MfaChallengeParams { FactorId = enrollment.Id }))!;
        var verify = () => this.Client.Verify(new MfaVerifyParams { Code = "", ChallengeId = challenge.Id, FactorId = "" });
        await verify.Should().ThrowAsync<GotrueException>();
    }
}

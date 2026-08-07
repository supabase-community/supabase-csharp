#region

using System.Threading.Tasks;
using FluentAssertions;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue.Mfa;
using static Gotrue.Tests.TestUtils;

#endregion

namespace Gotrue.Tests.Stateless;

/// <summary>
///     End-to-end TOTP multi-factor flow through the stateless client against the live stack: enrolling and
///     verifying a factor to reach AAL2 (threading the access token through every call), then unenrolling back
///     to AAL1.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StatelessMfaTests : StatelessFixture
{
    [TestMethod]
    public async Task Totp_ShouldRaiseAssuranceToAal2AndReturnToAal1OnUnenroll()
    {
        var email = RandomEmail();
        await this.Client.SignUp(email, Password, Options);
        var session = (await this.Client.SignIn(email, Password, Options))!;
        session.AccessToken.Should().NotBeNull();
        var enrollment = (await this.Client.Enroll(session.AccessToken, new MfaEnrollParams
        {
            FactorType = "totp",
            Issuer = "Supabase",
            FriendlyName = "Enroll test",
        }, Options))!;
        var challenge = (await this.Client.Challenge(session.AccessToken, new MfaChallengeParams { FactorId = enrollment.Id }, Options))!;
        challenge.Id.Should().NotBeNull();
        var verified = (await this.Client.Verify(session.AccessToken, new MfaVerifyParams
        {
            FactorId = enrollment.Id,
            ChallengeId = challenge.Id,
            Code = TotpCode(enrollment),
        }, Options))!;
        verified.AccessToken.Should().NotBeNull();
        await this.Client.SignOut(session.AccessToken!, Options);
        session = (await this.Client.SignIn(email, Password, Options))!;
        var afterSignIn = (await this.Client.GetAuthenticatorAssuranceLevel(session.AccessToken!, Options))!;
        afterSignIn.CurrentLevel.Should().Be(AuthenticatorAssuranceLevel.aal1);
        afterSignIn.NextLevel.Should().Be(AuthenticatorAssuranceLevel.aal2);
        var elevatedSession = (await this.Client.ChallengeAndVerify(session.AccessToken!, new MfaChallengeAndVerifyParams
        {
            FactorId = enrollment.Id,
            Code = TotpCode(enrollment),
        }, Options))!;
        var elevated = (await this.Client.GetAuthenticatorAssuranceLevel(elevatedSession.AccessToken!, Options))!;
        elevated.CurrentLevel.Should().Be(AuthenticatorAssuranceLevel.aal2);
        elevated.NextLevel.Should().Be(AuthenticatorAssuranceLevel.aal2);
        (await this.Client.ListFactors(session.AccessToken!, Options))!.Totp.Should().ContainSingle();
        await this.Client.Unenroll(session.AccessToken!, new MfaUnenrollParams { FactorId = enrollment.Id }, Options);
        await this.Client.SignOut(session.AccessToken!, Options);
        session = (await this.Client.SignIn(email, Password, Options))!;
        var afterUnenroll = (await this.Client.GetAuthenticatorAssuranceLevel(session.AccessToken!, Options))!;
        afterUnenroll.CurrentLevel.Should().Be(AuthenticatorAssuranceLevel.aal1);
        afterUnenroll.NextLevel.Should().Be(AuthenticatorAssuranceLevel.aal1);
        (await this.Client.ListFactors(session.AccessToken!, Options))!.Totp.Should().BeEmpty();
    }

    private static string TotpCode(MfaEnrollResponse enrollment) =>
        TotpGenerator.GeneratePin(enrollment.Totp!.Secret, 30, 6);
}

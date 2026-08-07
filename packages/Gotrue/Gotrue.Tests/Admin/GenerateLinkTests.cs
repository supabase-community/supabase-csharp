#region

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using static Gotrue.Tests.TestUtils;

#endregion

namespace Gotrue.Tests.Admin;

/// <summary>
///     End-to-end service-role generation of action links against the live stack: the verification type the
///     server assigns for magic-link, recovery, and email-change links.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class GenerateLinkTests : AdminFixture
{
    [TestMethod]
    public async Task GenerateLink_ShouldReturnSignupThenMagicLink_GivenMagicLinkForNewThenExistingUser()
    {
        var options = new GenerateLinkOptions(GenerateLinkOptions.LinkType.MagicLink, RandomEmail());
        (await this.Admin.GenerateLink(options))!.VerificationType.Should().Be("signup",
            "a magic link for an unknown user provisions the account as a signup");
        (await this.Admin.GenerateLink(options))!.VerificationType.Should().Be("magiclink");
    }

    [TestMethod]
    public async Task GenerateLink_ShouldReturnRecovery_GivenRecoveryLinkForKnownUser()
    {
        var email = RandomEmail();
        (await this.Admin.GenerateLink(new GenerateLinkOptions(GenerateLinkOptions.LinkType.MagicLink, email)))!
            .VerificationType.Should().Be("signup");
        (await this.Admin.GenerateLink(new GenerateLinkOptions(GenerateLinkOptions.LinkType.Recovery, email)))!
            .VerificationType.Should().Be("recovery");
    }

    [TestMethod]
    public async Task GenerateLink_ShouldReturnEmailChangeTypes_GivenEmailChangeLinks()
    {
        var email = RandomEmail();
        var newEmail = RandomEmail();
        await this.Admin.CreateUser(new AdminUserAttributes { Email = email });
        (await this.Admin.GenerateLink(new GenerateLinkEmailChangeCurrentOptions(email, newEmail)))!
            .VerificationType.Should().Be("email_change_current");
        (await this.Admin.GenerateLink(new GenerateLinkEmailChangeNewOptions(email, newEmail)))!
            .VerificationType.Should().Be("email_change_new");
    }
}

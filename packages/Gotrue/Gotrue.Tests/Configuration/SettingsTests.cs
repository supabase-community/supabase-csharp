#region

using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Gotrue.Tests.Configuration;

/// <summary>
///     End-to-end retrieval of the GoTrue instance settings against the live stack: the client surfaces the
///     server's enabled providers and sign-up configuration.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class SettingsTests
{
    [TestMethod]
    public async Task Settings_ShouldReturnServerProviderAndSignUpConfiguration()
    {
        var settings = await TestClients.AgainstCliStack().Settings();
        settings.Should().NotBeNull();
        using (new AssertionScope())
        {
            settings!.ExternalProviders!["email"].Should().BeTrue();
            settings.ExternalProviders["zoom"].Should().BeFalse();
            settings.DisableSignup.Should().BeFalse();
            settings.MailerAutoConfirm.Should().BeTrue();
            settings.PhoneAutoConfirm.Should().BeTrue();
        }
    }
}

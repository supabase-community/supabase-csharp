#region

using System.Threading.Tasks;
using FluentAssertions;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Supabase.Gotrue.Constants.AuthState;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     End-to-end sign-out against the live stack: signing out clears the current session and user, and a
///     subsequent sign-up establishes a distinct identity.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class SignOutTests : AuthClientFixture
{
    [TestMethod]
    public async Task SignOut_ShouldClearSessionAndUser()
    {
        await this.SignUpNewUser();
        this.StateChanges.Should().Contain(SignedIn);
        this.StateChanges.Clear();
        await this.Client.SignOut();
        this.VerifySignedOut();
    }

    [TestMethod]
    public async Task SignUp_ShouldEstablishDistinctIdentity_GivenPriorSignOut()
    {
        var firstUser = await this.SignUpNewUser();
        this.StateChanges.Clear();
        await this.Client.SignOut();
        this.VerifySignedOut();
        this.StateChanges.Clear();
        var secondUser = await this.SignUpNewUser();
        this.StateChanges.Should().Contain(SignedIn);
        secondUser.User!.Id.Should().NotBe(firstUser.User!.Id);
    }
}

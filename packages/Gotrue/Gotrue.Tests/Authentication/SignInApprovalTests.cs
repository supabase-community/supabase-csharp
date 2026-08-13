#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the exact bytes each SignIn variant puts on the wire. This is the transport contract the
///     Newtonsoft-to-System.Text.Json migration must preserve: property casing, null omission, enum mapping
///     and value formatting all live in the captured payload, not in any DTO's attributes.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class SignInApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task SignInRequest_ShouldSerializeToExpectedPayload_GivenEmailAndPassword()
    {
        await this.Api.SignInWithEmail("user@example.com", "super-secret-password");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task SignInRequest_ShouldSerializeToExpectedPayload_GivenPhoneAndPassword()
    {
        await this.Api.SignInWithPhone("+15555550123", "super-secret-password");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task SignInRequest_ShouldSerializeToExpectedPayload_GivenIdToken()
    {
        await this.Api.SignInWithIdToken(Constants.Provider.Google, "id-token-value");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task SignInRequest_ShouldSerializeToExpectedPayload_GivenAnonymousWithData()
    {
        await this.Api.SignInAnonymously(
            new SignInAnonymouslyOptions { Data = new() { { "invited_by", "ada" } } });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

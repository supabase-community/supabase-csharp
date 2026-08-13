#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the exact bytes each SignUp variant puts on the wire, including how optional user metadata
///     nests under <c>data</c>. This is the transport contract the System.Text.Json migration must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class SignUpApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task SignUpRequest_ShouldSerializeToExpectedPayload_GivenEmailAndPassword()
    {
        await this.Api.SignUpWithEmail("user@example.com", "super-secret-password");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task SignUpRequest_ShouldNestMetadataUnderData_GivenUserData()
    {
        await this.Api.SignUpWithEmail("user@example.com", "super-secret-password",
            new SignUpOptions { Data = new() { { "first_name", "Ada" }, { "age", 36 } } });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task SignUpRequest_ShouldSerializeToExpectedPayload_GivenPhoneAndPassword()
    {
        await this.Api.SignUpWithPhone("+15555550123", "super-secret-password");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Users;

/// <summary>
///     Pins the exact bytes UpdateUser puts on the wire. <see cref="UserAttributes" /> serializes every
///     property, so unset fields go out as explicit <c>null</c> and the metadata bag as <c>{}</c> — precisely
///     the null-omission behaviour the System.Text.Json migration changes by default. Recorded here as-is.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class UpdateUserApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task UpdateUserRequest_ShouldSerializeAllAttributes_GivenEmailOnly()
    {
        await this.Api.UpdateUser("user-jwt", new UserAttributes { Email = "new@example.com" });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

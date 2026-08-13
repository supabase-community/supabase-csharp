#region

using System;
using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins the exact bytes SignInWithSSO puts on the wire. The payload carries a boolean
///     (<c>skip_http_redirect</c>) and explicit nulls (<c>redirect_to</c>, <c>code_challenge</c>) on the
///     non-PKCE path — both are serialization behaviours the System.Text.Json migration can silently change.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class SingleSignOnApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task SsoRequest_ShouldSerializeToExpectedPayload_GivenProviderId()
    {
        await this.Api.SignInWithSSO(new Guid("11111111-1111-1111-1111-111111111111"));
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task SsoRequest_ShouldSerializeToExpectedPayload_GivenDomain()
    {
        await this.Api.SignInWithSSO("example.com");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

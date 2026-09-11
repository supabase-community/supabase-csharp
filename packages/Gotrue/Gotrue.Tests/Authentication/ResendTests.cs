using System.Net;
using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using static Gotrue.Tests.TestUtils;

namespace Gotrue.Tests.Authentication;

[TestClass]
[TestCategory("E2E")]
public class ResendTests : AuthClientFixture
{
    [TestMethod]
    public async Task Resend_ShouldRequestConfirmationCode_GivenSignUpType()
    {
        var email = RandomEmail();
        var resend = new ResendParam(Constants.ResendType.SignUp) { Email = email };
        var response = await this.Client.Resend(resend);

        Assert.IsNotNull(response);
        Assert.AreEqual(HttpStatusCode.OK, response?.ResponseMessage?.StatusCode);
    }

    [TestMethod]
    public async Task Resend_ShouldRequestConfirmationCode_GivenSmsType()
    {
        var resend = new ResendParam(Constants.ResendType.Sms) { Phone = "5544989899898" };
        var response = await this.Client.Resend(resend);

        Assert.IsNotNull(response);
        Assert.AreEqual(HttpStatusCode.OK, response?.ResponseMessage?.StatusCode);
    }

    [TestMethod]
    public async Task Resend_ShouldRequestConfirmationCode_GivenPhoneChangeType()
    {
        var resend = new ResendParam(Constants.ResendType.PhoneChange) { Phone = "5544989899898" };
        var response = await this.Client.Resend(resend);

        Assert.IsNotNull(response);
        Assert.AreEqual(HttpStatusCode.OK, response?.ResponseMessage?.StatusCode);
    }

    [TestMethod]
    public async Task Resend_ShouldRequestConfirmationCode_GivenEmailChangeType()
    {
        var resend = new ResendParam(Constants.ResendType.EmailChange) { Email = RandomEmail() };
        var response = await this.Client.Resend(resend);

        Assert.IsNotNull(response);
        Assert.AreEqual(HttpStatusCode.OK, response?.ResponseMessage?.StatusCode);
    }
}

#region

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Gotrue.Tests.TestUtils;
using static Supabase.Gotrue.Constants.AuthState;

#endregion

namespace Gotrue.Tests.Authentication;

/// <summary>
///     Pins how SignUp treats the session the server returns. With email confirmations disabled the user is
///     auto-confirmed via <c>email_confirmed_at</c> (not <c>confirmed_at</c>), and the client must adopt that
///     session even though <c>AllowUnconfirmedUserSessions</c> is left at its default of false (issue #130).
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class SignUpContractTests
{
    // A signup response for an auto-confirmed user: a usable session whose user is confirmed through
    // email_confirmed_at with confirmed_at absent — the shape a stack with enable_confirmations = false returns.
    private const string AutoConfirmedSignUp =
        """
        {
          "access_token": "auto-confirmed-access-token",
          "refresh_token": "auto-confirmed-refresh-token",
          "token_type": "bearer",
          "expires_in": 3600,
          "user": {
            "id": "user-id-130",
            "aud": "authenticated",
            "email": "auto@example.com",
            "email_confirmed_at": "2026-06-26T06:08:11Z"
          }
        }
        """;

    private readonly List<Constants.AuthState> stateChanges = new();
    private IGotrueClient<User, Session> client = null!;
    private MockGotrueServer server = null!;

    [TestInitialize]
    public void TestInitializer()
    {
        server = new MockGotrueServer();
        client = TestClients.Against(server);
        client.AddStateChangedListener((_, state) => stateChanges.Add(state));
    }

    [TestCleanup]
    public void TestCleanup() => server.Dispose();

    [TestMethod]
    public async Task SignUp_ShouldAdoptTheSession_GivenAutoConfirmedUserAndDefaultOptions()
    {
        server.Given(Request.Create().WithPath("/signup").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(AutoConfirmedSignUp));
        await client.SignUp(RandomEmail(), Password);
        using (new AssertionScope())
        {
            client.CurrentSession.Should().NotBeNull(
                "an auto-confirmed signup returns a usable session the client must adopt even with AllowUnconfirmedUserSessions off (issue #130)");
            client.CurrentUser!.Id.Should().Be("user-id-130");
            stateChanges.Should().Contain(SignedIn);
        }
    }
}

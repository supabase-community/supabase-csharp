#region

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Supabase.Gotrue.Constants;

#endregion

namespace Gotrue.Tests.Pkce;

/// <summary>
///     Pins the PKCE wire contract for the flows that use it: the SDK keeps the raw code verifier and sends
///     only its SHA-256 code challenge to the server, and the OAuth authorize URL carries the CSRF state
///     (auto-generated or developer-supplied). Asserted against a stubbed server (RFC 7636).
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class PkceContractTests
{
    private IGotrueClient<User, Session> client = null!;
    private MockGotrueServer server = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        server = new MockGotrueServer();
        client = TestClients.Against(server);
    }

    [TestCleanup]
    public void TestCleanup() => server.Dispose();

    [TestMethod]
    public async Task SignInWithOtp_ShouldSendSha256OfVerifierAsCodeChallenge_GivenPkceFlow()
    {
        StubJsonOk("/otp");
        var state = await client.SignInWithOtp(new SignInWithPasswordlessEmailOptions("test@example.com")
        {
            FlowType = OAuthFlowType.PKCE,
        });
        var request = server.VerifySingleReceivedRequest().WithMethod(HttpMethod.Post).WithPath("/otp");
        request.ReadJsonBodyField("code_challenge_method").Should().Be("s256");
        request.ReadJsonBodyField("code_challenge").Should().Be(S256(state.PKCEVerifier!),
            "the server must receive BASE64URL(SHA256(verifier)) per RFC 7636 §4.2");
    }

    [TestMethod]
    public async Task ResetPasswordForEmail_ShouldSendSha256OfVerifierAsCodeChallenge_GivenPkceFlow()
    {
        StubJsonOk("/recover");
        var state = await client.ResetPasswordForEmail(new ResetPasswordForEmailOptions("test@example.com")
        {
            FlowType = OAuthFlowType.PKCE,
        });
        var request = server.VerifySingleReceivedRequest().WithMethod(HttpMethod.Post).WithPath("/recover");
        request.ReadJsonBodyField("code_challenge_method").Should().Be("s256");
        request.ReadJsonBodyField("code_challenge").Should().Be(S256(state.PKCEVerifier!),
            "the server must receive BASE64URL(SHA256(verifier)) per RFC 7636 §4.2");
    }

    [TestMethod]
    public async Task SignIn_ShouldAutoGenerateStateInAuthUrl_GivenNoStateProvided()
    {
        var result = await client.SignIn(Provider.Github, new SignInOptions { FlowType = OAuthFlowType.PKCE });
        result.State.Should().NotBeNullOrEmpty();
        result.Uri.Query.Should().Contain($"state={result.State}");
    }

    [TestMethod]
    public async Task SignIn_ShouldUseDeveloperProvidedStateInAuthUrl_GivenStateProvided()
    {
        var customState = "my-server-generated-csrf-token";
        var result = await client.SignIn(Provider.Github, new SignInOptions
        {
            FlowType = OAuthFlowType.PKCE,
            State = customState,
        });
        result.State.Should().Be(customState);
        result.Uri.Query.Should().Contain($"state={customState}");
    }

    private void StubJsonOk(string path) =>
        server
            .Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

    private static string S256(string verifier)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(verifier));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}

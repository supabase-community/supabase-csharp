#region

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;

#endregion

namespace Gotrue.Tests.Nonces;

/// <summary>
///     Pins the PKCE / native sign-in nonce helpers: the code verifier the SDK generates, its BASE64URL
///     SHA-256 code challenge (RFC 7636 §4.2), and the hex SHA-256 used for Apple/Google raw nonces. These
///     are pure functions, so they are tested directly rather than through a client.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class NonceTests
{
    [TestMethod]
    public void GenerateNonce_ShouldReturn128UrlSafeCharacters()
    {
        var nonce = Helpers.GenerateNonce();
        nonce.Should().HaveLength(128).And.MatchRegex("^[a-z1-9]+$",
            "the verifier must stay within the unreserved PKCE character set (RFC 7636 §4.1)");
    }

    [TestMethod]
    public void GenerateNonce_ShouldReturnADifferentValueEachCall()
    {
        Helpers.GenerateNonce().Should().NotBe(Helpers.GenerateNonce(),
            "a reused code verifier would defeat the PKCE flow's protection");
    }

    [TestMethod]
    public void GeneratePKCENonceVerifier_ShouldReturnBase64UrlSha256OfTheVerifier()
    {
        Helpers.GeneratePKCENonceVerifier("hello_world_nonce")
            .Should().Be("9TMmi4JOlYOQEP2Ha39WXj9pySILGnAfQsz-yXws0yE",
                "the challenge is BASE64URL(SHA256(verifier)) with '+'->'-', '/'->'_', and padding trimmed");
    }

    [TestMethod]
    public void GeneratePKCENonceVerifier_ShouldEmitUrlSafeBase64WithoutPadding()
    {
        Helpers.GeneratePKCENonceVerifier(Helpers.GenerateNonce())
            .Should().MatchRegex("^[A-Za-z0-9_-]+$", "'+', '/' and '=' are not URL-safe and must be replaced or trimmed");
    }

    [TestMethod]
    public void GenerateSHA256NonceFromRawNonce_ShouldReturnLowercaseHexSha256()
    {
        Helpers.GenerateSHA256NonceFromRawNonce("hello_world_nonce")
            .Should().Be("f533268b824e95839010fd876b7f565e3f69c9220b1a701f42ccfec97c2cd321",
                "Apple/Google native sign-in expects the raw nonce hashed as a 64-char lowercase hex string");
    }
}

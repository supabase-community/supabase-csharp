using static Supabase.Gotrue.Constants;

namespace Supabase.Gotrue
{
    /// <summary>
    ///     Immutable inputs for linking a native OIDC identity to the currently signed-in user with an ID token.
    ///     Bundles the provider and token with the optional proofs GoTrue may require, so the linking surface stays
    ///     additive: new optional inputs are added here without changing the method signature.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var options = new LinkIdentityWithIdTokenOptions(Constants.Provider.Google, googleIdToken);
    ///     var session = await client.LinkIdentityWithIdToken(options);
    ///     </code>
    /// </example>
    public class LinkIdentityWithIdTokenOptions
    {
        /// <summary>
        ///     The OIDC provider that issued the ID token. Linking with an ID token is only defined for the native
        ///     providers: <see cref="Constants.Provider.Google" />, <see cref="Constants.Provider.Apple" />,
        ///     <see cref="Constants.Provider.Azure" />, and <see cref="Constants.Provider.Facebook" />.
        /// </summary>
        public Provider Provider { get; }

        /// <summary>
        ///     The OIDC ID token issued by <see cref="Provider" />. Its <c>iss</c> claim must match the provider.
        /// </summary>
        public string IdToken { get; }

        /// <summary>
        ///     If the ID token contains an <c>at_hash</c> claim, the hash of this value is compared to the value in
        ///     the ID token.
        /// </summary>
        public string? AccessToken { get; }

        /// <summary>
        ///     If the ID token contains a <c>nonce</c> claim, the hash of this value is compared to the value in the
        ///     ID token.
        /// </summary>
        public string? Nonce { get; }

        /// <summary>
        ///     Verification token received when the user completes the captcha on the site.
        /// </summary>
        public string? CaptchaToken { get; }

        /// <param name="provider">The native OIDC provider that issued the ID token.</param>
        /// <param name="idToken">The OIDC ID token issued by <paramref name="provider" />.</param>
        /// <param name="accessToken">Access token whose hash is checked against the ID token's <c>at_hash</c> claim, when present.</param>
        /// <param name="nonce">Nonce whose hash is checked against the ID token's <c>nonce</c> claim, when present.</param>
        /// <param name="captchaToken">Verification token from completing the captcha on the site.</param>
        public LinkIdentityWithIdTokenOptions(Provider provider, string idToken, string? accessToken = null, string? nonce = null, string? captchaToken = null)
        {
            Provider = provider;
            IdToken = idToken;
            AccessToken = accessToken;
            Nonce = nonce;
            CaptchaToken = captchaToken;
        }
    }
}

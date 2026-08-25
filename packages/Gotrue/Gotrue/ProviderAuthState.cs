using System;
namespace Supabase.Gotrue
{
    /// <summary>
    /// Represents an OAuth Provider's URI and Parameters.
    ///
    /// For use with Provider Auth, PKCE Auth, and ID Token auth.
    /// </summary>
    public class ProviderAuthState
    {
        /// <summary>
        /// The Generated Provider's URI
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// The PKCE Verifier nonce, only set during a PKCE auth flow.
        /// </summary>
        public string? PKCEVerifier { get; set; }

        /// <summary>
        /// The state parameter included in the OAuth URL for CSRF protection (RFC 6749 §10.12).
        /// Validate this against the state echoed back in the OAuth callback.
        /// </summary>
        [Obsolete("Provider-side OAuth state is managed by the GoTrue server; it is no longer sent to the authorize endpoint (issue #377). This value is generated locally but has no effect on sign-in. For server-side CSRF, carry your token via SignInOptions.RedirectTo. This property is non-functional and will be removed in v8.")]
        public string State { get; set; } = null!;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uri"></param>
        public ProviderAuthState(Uri uri)
        {
            Uri = uri;
        }
    }
}

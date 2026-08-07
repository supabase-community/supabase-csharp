using System.Collections.Generic;
using static Supabase.Gotrue.Constants;

namespace Supabase.Gotrue
{
    /// <summary>
    /// Options used for signing in a user.
    /// </summary>
    public class SignInOptions
    {
        /// <summary>
        /// A URL or mobile address to send the user to after they are confirmed.
        /// </summary>
        public string? RedirectTo { get; set; }

        /// <summary>
        /// A space-separated list of scopes granted to the OAuth application.
        /// </summary>
        public string? Scopes { get; set; }

        /// <summary>
        /// An object of key-value pairs containing query parameters granted to the OAuth application.
        /// </summary>
        public Dictionary<string, string>? QueryParams { get; set; }

        /// <summary>
        /// Represents an OAuth Flow type, defaults to `Implicit`
        ///
        /// PKCE is recommended for mobile and server-side applications.
        /// </summary>
        public OAuthFlowType FlowType { get; set; } = OAuthFlowType.Implicit;

        /// <summary>
        /// An optional state parameter for CSRF protection (RFC 6749 §10.12).
        /// If not provided, one will be generated automatically.
        /// Store the returned <see cref="ProviderAuthState.State"/> value and validate it
        /// against the state echoed back in the OAuth callback.
        /// </summary>
        public string? State { get; set; }
    }
}

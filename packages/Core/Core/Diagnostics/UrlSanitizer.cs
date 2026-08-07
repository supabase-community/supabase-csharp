using System;
using System.Text;

namespace Supabase.Core.Diagnostics
{
    /// <summary>
    /// Reduces URLs to a form that is safe to record in telemetry and logs.
    ///
    /// Supabase URLs routinely carry secrets outside the path — access tokens in fragments,
    /// API keys, PKCE verifiers and grant parameters in query strings — so telemetry must only
    /// ever record scheme, host, port and path. There is no overload that keeps the query
    /// string; that is deliberate.
    /// </summary>
    public static class UrlSanitizer
    {
        /// <summary>
        /// Returns <c>scheme://host[:port]/path</c> for the given URI, dropping user info, the
        /// query string, and the fragment. Default ports are omitted.
        /// </summary>
        /// <param name="uri">The URI to reduce to scheme, host, port and path.</param>
        public static string Sanitize(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
                return StripAfterPath(uri.OriginalString);

            var builder = new StringBuilder();
            builder.Append(uri.Scheme).Append("://").Append(uri.Host);
            if (!uri.IsDefaultPort)
                builder.Append(':').Append(uri.Port);

            builder.Append(uri.AbsolutePath);
            return builder.ToString();
        }

        /// <summary>
        /// Sanitizes a URL provided as a string; returns the input truncated at the first
        /// query/fragment delimiter when it cannot be parsed as a URI.
        /// </summary>
        /// <param name="url">The URL to sanitize; parsed as an absolute URI when possible.</param>
        public static string Sanitize(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? Sanitize(uri) : StripAfterPath(url);

        private static string StripAfterPath(string url)
        {
            var delimiterIndex = url.IndexOfAny(['?', '#']);
            return delimiterIndex < 0 ? url : url.Substring(0, delimiterIndex);
        }
    }
}

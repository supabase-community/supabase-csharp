using System.Linq;
using static Supabase.Storage.Exceptions.FailureHint.Reason;

namespace Supabase.Storage.Exceptions
{
    /// <summary>
    /// Maps a failed storage response onto a coarse <see cref="Reason"/> a caller can branch on.
    /// </summary>
    public static class FailureHint
    {
        /// <summary>
        /// A coarse classification of why a storage request failed, derived from its status code and body.
        /// </summary>
        public enum Reason
        {
            /// <summary>
            /// The failure could not be attributed to a known cause — the status and body matched no
            /// recognised pattern, or the response carried no body to inspect.
            /// </summary>
            Unknown,

            /// <summary>
            /// The request was rejected for authentication or authorization reasons (HTTP 401, or a
            /// 400/403 whose body names an auth cause such as a missing or malformed token).
            /// </summary>
            NotAuthorized,

            /// <summary>
            /// The storage service failed internally (HTTP 500).
            /// </summary>
            Internal,

            /// <summary>
            /// The requested object or bucket does not exist (HTTP 404).
            /// </summary>
            NotFound,

            /// <summary>
            /// The resource being created already exists (HTTP 409).
            /// </summary>
            AlreadyExists,

            /// <summary>
            /// The request was rejected as invalid (an HTTP 400 whose body indicates invalid input).
            /// </summary>
            InvalidInput,

            /// <summary>
            /// The upload exceeded the gateway's request-size limit (HTTP 413). Retry it through a
            /// resumable upload (<c>UploadOrResume</c>) rather than a single request.
            /// </summary>
            EntityTooLarge
        }

        /// <summary>
        /// Classifies a failed storage request from its status code and response body.
        /// </summary>
        /// <param name="storageException">The failure to classify.</param>
        /// <returns>The matching <see cref="Reason"/>, or <see cref="Reason.Unknown"/> when nothing matches.</returns>
        public static Reason DetectReason(SupabaseStorageException storageException)
        {
            if (storageException.Content == null)
                return Unknown;

            return storageException.StatusCode switch
            {
                400 when storageException.Content.ToLower().Contains("authorization") => NotAuthorized,
                400 when storageException.Content.ToLower().Contains("malformed") => NotAuthorized,
                400 when storageException.Content.ToLower().Contains("invalid signature") => NotAuthorized,
                400 when storageException.Content.ToLower().Contains("invalid") => InvalidInput,
                401 => NotAuthorized,
                403 when storageException.Content.ToLower().Contains("invalid compact jws") => NotAuthorized,
                403 when storageException.Content.ToLower().Contains("signature verification failed") => NotAuthorized,
                404 when storageException.Content.ToLower().Contains("not found") => NotFound,
                409 when storageException.Content.ToLower().Contains("exists") => AlreadyExists,
                413 => EntityTooLarge,
                500 => Internal,
                _ => Unknown
            };
        }
    }

}

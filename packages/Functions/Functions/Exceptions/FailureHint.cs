using static Supabase.Functions.Exceptions.FailureHint.Reason;

namespace Supabase.Functions.Exceptions
{
    /// <summary>
    /// A hint as to why a request failed.
    /// </summary>
    public static class FailureHint
    {
        /// <summary>
        /// A failure reason
        /// </summary>
        public enum Reason
        {
            /// <summary>
            /// An unknown reason
            /// </summary>
            Unknown,
            /// <summary>
            /// Request was not authorized
            /// </summary>
            NotAuthorized,
            /// <summary>
            /// An internal error occurred, check your supabase logs.
            /// </summary>
            Internal,
        }

        /// <summary>
        /// Attempts to detect a reason given an exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Reason DetectReason(FunctionsException ex)
        {
            if (ex.Content == null)
                return Unknown;

            return ex.StatusCode switch
            {
                401 => NotAuthorized,
                403 when ex.Content.Contains("apikey") => NotAuthorized,
                500 => Internal,
                _ => Unknown
            };
        }
    }
}
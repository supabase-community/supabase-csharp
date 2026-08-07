using System;
using System.Net.Http;

namespace Supabase.Functions.Exceptions
{
    /// <summary>
    /// An Exception thrown within <see cref="Functions"/>
    /// </summary>
    public class FunctionsException : Exception
    {
        /// <inheritdoc />
        public FunctionsException(string? message) : base(message) { }

        /// <inheritdoc />
        public FunctionsException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// The Http Response
        /// </summary>
        public HttpResponseMessage? Response { get; internal set; }

        /// <summary>
        /// The Http response content
        /// </summary>
        public string? Content { get; internal set; }

        /// <summary>
        /// The Http Status code
        /// </summary>
        public int StatusCode { get; internal set; }

        /// <summary>
        /// A parsed reason for a given failure
        /// </summary>
        public FailureHint.Reason Reason { get; internal set; }

        /// <summary>
        /// Attempts to detect a reason for this exception
        /// </summary>
        public void AddReason()
        {
            Reason = FailureHint.DetectReason(this);
        }
    }
}
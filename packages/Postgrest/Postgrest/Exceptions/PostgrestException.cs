using System;
using System.Net.Http;
namespace Supabase.Postgrest.Exceptions
{
	/// <summary>
	/// Errors from Postgrest are wrapped by this exception
	/// </summary>
	public class PostgrestException : Exception
	{
		/// <inheritdoc />
		public PostgrestException(string? message) : base(message) { }
		/// <inheritdoc />
		public PostgrestException(string? message, Exception? innerException) : base(message, innerException) { }

		/// <summary>
		/// The response object from Postgrest
		/// </summary>
		public HttpResponseMessage? Response { get; internal set; }

		/// <summary>
		/// The content of the response object from Postgrest
		/// </summary>
		public string? Content { get; internal set; }

		/// <summary>
		/// The HTTP status code of the response object from Postgrest
		/// </summary>
		public int StatusCode { get; internal set; }

		/// <summary>
		/// Postgres client's best effort at decoding the error from the GoTrue server.
		/// </summary>
		public FailureHint.Reason Reason { get; internal set; }

		/// <summary>
		/// Attempts to decode the error from the GoTrue server.
		/// </summary>
		public void AddReason()
		{
			Reason = FailureHint.DetectReason(this);
		}
	}
}

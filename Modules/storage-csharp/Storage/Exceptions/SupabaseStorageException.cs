using System;
using System.Net.Http;

namespace Supabase.Storage.Exceptions
{
    public class SupabaseStorageException : Exception
    {
        public SupabaseStorageException(string? message) : base(message) { }
        public SupabaseStorageException(string? message, Exception? innerException) : base(message, innerException) { }

        public HttpResponseMessage? Response { get; internal set; }

        public string? Content { get; internal set; }

        public int StatusCode { get; internal set; }

        public FailureHint.Reason Reason { get; private set; }

        public void AddReason()
        {
            Reason = FailureHint.DetectReason(this);
        }
    }
}
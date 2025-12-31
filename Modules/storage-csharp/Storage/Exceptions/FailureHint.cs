using System.Linq;
using static Supabase.Storage.Exceptions.FailureHint.Reason;

namespace Supabase.Storage.Exceptions
{
    public static class FailureHint
    {
        public enum Reason
        {
            Unknown,
            NotAuthorized,
            Internal,
            NotFound,
            AlreadyExists,
            InvalidInput
        }

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
                500 => Internal,
                _ => Unknown
            };
        }
    }

}

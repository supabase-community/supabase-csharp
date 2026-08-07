using System.Collections.Generic;
namespace Supabase.Gotrue
{
    /// <summary>
    /// Options used for signing up a user.
    /// </summary>
    public class SignUpOptions : SignInOptions
    {
        /// <summary>
        /// Optional user metadata.
        /// </summary>
        public Dictionary<string, object>? Data { get; set; }
    }
}

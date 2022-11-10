using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase.Gotrue;

namespace Supabase
{
    /// <summary>
    /// Options available for Supabase Client Configuration
    /// </summary>
    public class SupabaseOptions
    {
        public string Schema = "public";

        /// <summary>
        /// Should the Client automatically handle refreshing the User's Token?
        /// </summary>
        public bool AutoRefreshToken { get; set; } = true;

        /// <summary>
        /// Should the Client Initialize Realtime?
        /// </summary>
        public bool ShouldInitializeRealtime { get; set; } = false;

        /// <summary>
        /// Should the Client automatically connect to Realtime?
        /// </summary>
        public bool AutoConnectRealtime { get; set; } = false;

        /// <summary>
        /// Should the Client call <see cref="SessionPersistor"/>, <see cref="SessionRetriever"/>, and <see cref="SessionDestroyer"/>?
        /// </summary>
        public bool PersistSession { get; set; } = true;

        /// <summary>
        /// Function called to persist the session (probably on a filesystem or cookie)
        /// </summary>
        public Func<Session, Task<bool>> SessionPersistor = (Session session) => Task.FromResult<bool>(true);

        /// <summary>
        /// Function to retrieve a session (probably from the filesystem or cookie)
        /// </summary>
        public Func<Task<Session?>> SessionRetriever = () => Task.FromResult<Session?>(null);

        /// <summary>
        /// Function to destroy a session.
        /// </summary>
        public Func<Task<bool>> SessionDestroyer = () => Task.FromResult<bool>(true);

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public string AuthUrlFormat { get; set; } = "{0}/auth/v1";
        public string RestUrlFormat { get; set; } = "{0}/rest/v1";
        public string RealtimeUrlFormat { get; set; } = "{0}/realtime/v1";
        public string StorageUrlFormat { get; set; } = "{0}/storage/v1";

        public string FunctionsUrlFormat { get; set; } = "{0}/functions/v1";
    }
}

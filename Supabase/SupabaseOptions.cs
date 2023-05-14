using Supabase.Gotrue;
using Supabase.Interfaces;
using System;
using System.Collections.Generic;

namespace Supabase
{
    /// <summary>
    /// Options available for Supabase Client Configuration
    /// </summary>
    public class SupabaseOptions
    {
        /// <summary>
        /// Schema to be used in Postgres / Realtime
        /// </summary>
        public string Schema = "public";

        /// <summary>
        /// Should the Client automatically handle refreshing the User's Token?
        /// </summary>
        public bool AutoRefreshToken { get; set; } = true;

        /// <summary>
        /// Should the Client automatically connect to Realtime?
        /// </summary>
        public bool AutoConnectRealtime { get; set; }

        /// <summary>
        /// Should the Client call <see cref="SessionPersistor"/>, <see cref="SessionRetriever"/>, and <see cref="SessionDestroyer"/>?
        /// </summary>
        public bool PersistSession { get; set; } = true;

        /// <summary>
        /// Functions passed to Gotrue that handle sessions. 
        /// 
        /// **By default these do nothing for persistence.**
        /// </summary>
        public IGotrueSessionPersistence<Session> SessionHandler { get; set; } = new DefaultSupabaseSessionHandler();

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        /// <summary>
        /// Specifies Options passed to the StorageClient.
        /// </summary>
        public Storage.ClientOptions StorageClientOptions { get; set; } = new Storage.ClientOptions();

        public string AuthUrlFormat { get; set; } = "{0}/auth/v1";
        public string RestUrlFormat { get; set; } = "{0}/rest/v1";
        public string RealtimeUrlFormat { get; set; } = "{0}/realtime/v1";
        public string StorageUrlFormat { get; set; } = "{0}/storage/v1";

        public string FunctionsUrlFormat { get; set; } = "{0}/functions/v1";
    }
}

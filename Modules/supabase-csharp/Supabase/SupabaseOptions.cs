using Supabase.Gotrue;
using System.Collections.Generic;
using Supabase.Gotrue.Interfaces;

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
        /// Functions passed to Gotrue that handle sessions. 
        /// 
        /// **By default these do nothing for persistence.**
        /// </summary>
        public IGotrueSessionPersistence<Session> SessionHandler { get; set; } = new DefaultSupabaseSessionHandler();

        /// <summary>
        /// Allows developer to specify options that will be passed to all child Supabase clients.
        /// </summary>
        public Dictionary<string, string> Headers = new();

        /// <summary>
        /// Specifies Options passed to the StorageClient.
        /// </summary>
        public Storage.ClientOptions StorageClientOptions { get; set; } = new();

        /// <summary>
        /// The Supabase Auth Url Format
        /// </summary>
        public string AuthUrlFormat { get; set; } = "{0}/auth/v1";
        
        /// <summary>
        /// The Supabase Postgrest Url Format
        /// </summary>
        public string RestUrlFormat { get; set; } = "{0}/rest/v1";
        
        /// <summary>
        /// The Supabase Realtime Url Format
        /// </summary>
        public string RealtimeUrlFormat { get; set; } = "{0}/realtime/v1";
        
        /// <summary>
        /// The Supabase Storage Url Format
        /// </summary>
        public string StorageUrlFormat { get; set; } = "{0}/storage/v1";

        /// <summary>
        /// The Supabase Functions Url Format
        /// </summary>
        public string FunctionsUrlFormat { get; set; } = "{0}/functions/v1";
    }
}

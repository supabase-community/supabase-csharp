using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    public interface ISupabaseSessionHandler
    {
        /// <summary>
        /// Function called to persist the session (probably on a filesystem or cookie)
        /// </summary>
        Task<bool> SessionPersistor<TSession>(TSession session) where TSession : Session;

        /// <summary>
        /// Function to retrieve a session (probably from the filesystem or cookie)
        /// </summary>
        Task<TSession?> SessionRetriever<TSession>() where TSession : Session;

        /// <summary>
        /// Function to destroy a session.
        /// </summary>
        Task<bool> SessionDestroyer();
    }
}

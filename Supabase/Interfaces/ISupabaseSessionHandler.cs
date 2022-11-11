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
        Task<bool> SessionPersistor(Session session);

        /// <summary>
        /// Function to retrieve a session (probably from the filesystem or cookie)
        /// </summary>
        Task<Session?> SessionRetriever();

        /// <summary>
        /// Function to destroy a session.
        /// </summary>
        Task<bool> SessionDestroyer();
    }
}

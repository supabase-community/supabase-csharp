using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Supabase
{
    /// <summary>
    /// Represents the default session handler for Gotrue - it does nothing by default.
    /// </summary>
    public class DefaultSupabaseSessionHandler : IGotrueSessionPersistence<Session>
    {
        public void SaveSession(Session session)
        {
        }

        public void DestroySession()
        {
        }

        public Session? LoadSession()
        {
            return null;
        }
    }
}
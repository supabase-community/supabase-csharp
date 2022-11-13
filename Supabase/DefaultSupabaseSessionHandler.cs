using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Interfaces;

namespace Supabase
{
    /// <summary>
    /// Represents the default session handler for Gotrue - it does nothing by default.
    /// </summary>
    public class DefaultSupabaseSessionHandler : ISupabaseSessionHandler
    {
        public Task<bool> SessionPersistor<TSession>(TSession session) where TSession : Session => Task.FromResult(true);


        public Task<TSession?> SessionRetriever<TSession>() where TSession : Session => Task.FromResult<TSession?>(null);


        public Task<bool> SessionDestroyer() => Task.FromResult(true);
    }
}

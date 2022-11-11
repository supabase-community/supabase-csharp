using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Interfaces;

namespace Supabase
{
    public class DefaultSupabaseSessionHandler : ISupabaseSessionHandler
    {
        public Task<bool> SessionPersistor(Session session) => Task.FromResult<bool>(true);


        public Task<Session?> SessionRetriever() => Task.FromResult<Session?>(null);


        public Task<bool> SessionDestroyer() => Task.FromResult<bool>(true);
    }
}

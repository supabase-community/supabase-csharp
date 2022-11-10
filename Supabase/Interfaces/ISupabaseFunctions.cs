using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Supabase.Interfaces
{
    public interface ISupabaseFunctions
    {
        Task<string> Invoke(string functionName, Dictionary<string, object>? body = null);
        Task<T?> Invoke<T>(string functionName, Dictionary<string, object>? body = null) where T : class;
        Task<HttpContent> RawInvoke(string functionName, Dictionary<string, object>? body = null);
    }
}
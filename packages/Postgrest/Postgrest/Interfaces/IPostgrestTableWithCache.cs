using System.Threading;
using System.Threading.Tasks;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Requests;

namespace Supabase.Postgrest.Interfaces
{
    /// <summary>
    /// Client interface for Postgrest
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPostgrestTableWithCache<T> : IPostgrestTable<T> where T : BaseModel, new()
    {
        /// <summary>
        /// Performs a Get request, returning a <see cref="CacheBackedRequest{TModel}"/> which populates from the cache, if applicable.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new Task<CacheBackedRequest<T>> Get(CancellationToken cancellationToken = default);
    }
}
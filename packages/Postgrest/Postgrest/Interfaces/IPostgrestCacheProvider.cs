using System.Threading.Tasks;

namespace Supabase.Postgrest.Interfaces
{
    /// <summary>
    /// A caching provider than can be used by postgrest to store requests.
    /// </summary>
    public interface IPostgrestCacheProvider
    {
        /// <summary>
        /// Gets an item from a caching solution, should coerce into a datatype.
        ///
        /// This will most likely be a JSON deserialization approach.
        /// </summary>
        /// <param name="key">A reproducible key for a defined query.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Task<T?> GetItem<T>(string key);

        /// <summary>
        /// Sets an item within a caching solution, should store in a way that the data can be retrieved and coerced into a generic type by <see cref="GetItem{T}"/>
        ///
        /// This will most likely be a JSON serialization approach.
        /// </summary>
        /// <param name="key">A reproducible key for a defined query.</param>
        /// <param name="value">An object of serializable data.</param>
        /// <returns></returns>
        public Task SetItem(string key, object value);

        /// <summary>
        /// Clear an item within a caching solution by a key.
        /// </summary>
        /// <param name="key">A reproducible key for a defined query.</param>
        /// <returns></returns>
        public Task ClearItem(string key);

        /// <summary>
        /// An empty/clear cache implementation.
        /// </summary>
        /// <returns></returns>
        public Task Empty();
    }
}
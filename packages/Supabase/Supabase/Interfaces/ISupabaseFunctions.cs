using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Supabase.Interfaces
{
    /// <summary>
    /// Contract representing a wrapper <see cref="Functions"/> client.
    /// </summary>
    public interface ISupabaseFunctions
    {
        /// <summary>
        /// Invoke a supabase function
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="body"></param>
        /// <returns>String content from invoke</returns>
        Task<string> Invoke(string functionName, Dictionary<string, object>? body = null);
        
        /// <summary>
        /// Invoke a supabase function and deserialize data to a provided model. 
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="body"></param>
        /// <typeparam name="T">Model representing data that is compatible with <see cref="Newtonsoft"/></typeparam>
        /// <returns>The deserialized Model</returns>
        Task<T?> Invoke<T>(string functionName, Dictionary<string, object>? body = null) where T : class;
        
        /// <summary>
        /// Invoke a supabase function and return the <see cref="HttpContent"/> for the developer to parse.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="body"></param>
        /// <returns>The HTTP Content</returns>
        Task<HttpContent> RawInvoke(string functionName, Dictionary<string, object>? body = null);
    }
}
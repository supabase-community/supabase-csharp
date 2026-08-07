using System.Collections.Specialized;
using System.Web;

namespace Supabase.Storage.Extensions
{
    /// <summary>
    /// Translates <see cref="PurgeCacheOptions"/> into the query string the CDN purge endpoint expects.
    /// </summary>
    public static class PurgeCacheOptionsExtension
    {
        /// <summary>
        /// Transforms the options into a <see cref="NameValueCollection"/> to be appended to a purge URL.
        /// The <c>transformations</c> flag is only emitted when explicitly requested, mirroring the
        /// storage-js client, so that the default purges every cached version.
        /// </summary>
        /// <param name="options">The purge options to translate.</param>
        /// <returns>A query collection carrying the options, empty when none apply.</returns>
        public static NameValueCollection ToQueryCollection(this PurgeCacheOptions options)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (options.Transformations == true)
                query.Add("transformations", "true");

            return query;
        }

        /// <summary>
        /// Appends the options as a query string to <paramref name="baseUrl"/>, adding the <c>?</c>
        /// separator only when at least one option applies.
        /// </summary>
        /// <param name="options">The purge options, or <c>null</c> for none.</param>
        /// <param name="baseUrl">The purge endpoint URL without a query string.</param>
        /// <returns>The URL with the options appended.</returns>
        public static string ToPurgeUrl(this PurgeCacheOptions? options, string baseUrl)
        {
            var query = options?.ToQueryCollection().ToString();
            return string.IsNullOrEmpty(query) ? baseUrl : $"{baseUrl}?{query}";
        }
    }
}

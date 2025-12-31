using System.Collections.Specialized;
using System.Web;

namespace Supabase.Storage.Extensions
{
    public static class DownloadOptionsExtension
    {
        /// <summary>
        /// Transforms options into a NameValueCollection to be used with a <see cref="UriBuilder"/>
        /// </summary>
        /// <param name="download"></param>
        /// <returns></returns>
        public static NameValueCollection ToQueryCollection(this DownloadOptions download)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (download.FileName == null)
            {
                return query;
            }
            
            query.Add("download", string.IsNullOrEmpty(download.FileName) ? "true" : download.FileName);

            return query;
        }
    }
}
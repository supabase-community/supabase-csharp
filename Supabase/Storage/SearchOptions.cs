using System;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class SearchOptions
    {
        /// <summary>
        /// Number of files to be returned
        /// </summary>
        [JsonProperty("limit")]
        public int Limit { get; set; } = 100;

        /// <summary>
        /// Starting position of query
        /// </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Column to sort by. Can be any colum inside of a <see cref="FileObject"/>
        /// </summary>
        [JsonProperty("sortBy")]
        public SortBy SortBy { get; set; } = new SortBy { Column = "name", Order = "asc" };
    }
}

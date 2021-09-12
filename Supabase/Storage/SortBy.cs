using System;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class SortBy
    {
        [JsonProperty("column")]
        public string Column { get; set; }

        [JsonProperty("order")]
        public string Order { get; set; }
    }
}

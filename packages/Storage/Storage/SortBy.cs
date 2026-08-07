using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class SortBy
    {
        [JsonProperty("column")]
        public string? Column { get; set; } = "name";

        [JsonProperty("order")]
        public string? Order { get; set; } = "asc";
    }
}

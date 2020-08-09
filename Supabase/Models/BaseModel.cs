using System;
using Newtonsoft.Json;

namespace Supabase.Models
{
    public abstract class BaseModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("inserted_at")]
        public DateTime InsertedAt { get; set; } = new DateTime();

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; } = new DateTime();
    }
}

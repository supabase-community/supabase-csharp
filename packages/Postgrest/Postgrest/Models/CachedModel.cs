using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Postgrest.Models
{
    /// <summary>
    /// Represents a cacheable model
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class CachedModel<TModel> where TModel : BaseModel, new()
    {
        /// <summary>
        /// The stored Models
        /// </summary>
        [JsonProperty("response")] public List<TModel>? Models { get; set; }

        /// <summary>
        /// Cache time in UTC.
        /// </summary>
        [JsonProperty("cachedAt")] public DateTime CachedAt { get; set; }
    }
}
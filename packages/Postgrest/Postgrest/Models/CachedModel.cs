using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Postgrest.Models;

/// <summary>
/// Represents a cacheable model
/// </summary>
/// <typeparam name="TModel"></typeparam>
public class CachedModel<TModel> where TModel : BaseModel, new()
{
    /// <summary>
    /// The stored Models
    /// </summary>
    [JsonPropertyName("response")] public List<TModel>? Models { get; set; }

    /// <summary>
    /// Cache time in UTC.
    /// </summary>
    [JsonPropertyName("cachedAt")] public DateTime CachedAt { get; set; }
}

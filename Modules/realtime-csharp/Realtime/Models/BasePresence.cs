using Newtonsoft.Json;

namespace Supabase.Realtime.Models;

/// <summary>
/// Represents an arbitrary Presence response.
/// </summary>
public class BasePresence
{
    /// <summary>
    /// The ref for this event. (can be used to establish sequence)
    /// </summary>
    [JsonProperty("phx_ref")]
    public string? PhoenixRef { get; set; }

    /// <summary>
    /// The previous ref for this presence event (can be used to establish sequence)
    /// </summary>
    [JsonProperty("phx_ref_prev")]
    public string? PhoenixPrevRef { get; set; }

    /// <summary>
    /// Disables serialization of phoenix_ref
    /// </summary>
    /// <returns></returns>
    public bool ShouldSerializePhoenixRef() => false;

    /// <summary>
    /// Disables serialization of phoenix_prev_ref
    /// </summary>
    /// <returns></returns>
    public bool ShouldSerializePhoenixPrevRef() => false;
}
using System.Text.Json.Serialization;

namespace Supabase.Realtime.Models;

/// <summary>
/// Represents an arbitrary Presence response.
/// </summary>
public class BasePresence
{
    /// <summary>
    /// The ref for this event. (can be used to establish sequence)
    /// </summary>
    /// <remarks>
    /// Server-assigned: fresh models constructed for <c>Track()</c> never set this, so
    /// WhenWritingNull keeps it out of the outbound track/untrack payload entirely (only
    /// affects serialization - a real phx_ref from a server response still deserializes
    /// normally). The realtime server rejects track payloads carrying an explicit
    /// <c>phx_ref: null</c>, which is what System.Text.Json wrote here unconditionally
    /// before this fix (the Newtonsoft-only ShouldSerializePhoenixRef() convention this
    /// replaced was silently no-op'd by the STJ migration).
    /// </remarks>
    [JsonPropertyName("phx_ref")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhoenixRef { get; set; }

    /// <summary>
    /// The previous ref for this presence event (can be used to establish sequence)
    /// </summary>
    /// <remarks>See <see cref="PhoenixRef"/>.</remarks>
    [JsonPropertyName("phx_ref_prev")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhoenixPrevRef { get; set; }
}

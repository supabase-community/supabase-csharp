using System.Text.Json.Serialization;

namespace Supabase.Realtime.Presence;

/// <summary>
/// Options used to initialize Realtime Presence
/// </summary>
public class PresenceOptions
{
    /// <summary>
    /// key option is used to track presence payload across clients
    /// </summary>
    [JsonPropertyName("key")]
    public string PresenceKey { get; set; }

    /// <summary>
    /// Whether presence tracking is enabled for this channel join. Requires
    /// <c>enabled: true</c> to receive the initial <c>presence_state</c> sync.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Presence options.
    /// </summary>
    /// <param name="presenceKey"></param>
    public PresenceOptions(string presenceKey) => this.PresenceKey = presenceKey;

    /// <summary>
    /// Presence options.
    /// </summary>
    /// <param name="presenceKey"></param>
    /// <param name="enabled"></param>
    public PresenceOptions(string presenceKey, bool enabled)
    {
        this.PresenceKey = presenceKey;
        this.Enabled = enabled;
    }
}

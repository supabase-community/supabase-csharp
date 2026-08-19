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
    /// Whether presence is enabled on this channel. The realtime server's phx_join schema
    /// defaults this to false when omitted, and (since server v2.124.0) enforces it strictly:
    /// a channel joined without it silently drops track/untrack pushes instead of replying.
    /// Defaults to false here too, since a <see cref="RealtimeChannel"/> carries a placeholder
    /// <see cref="PresenceOptions"/> before <c>Register</c> is ever called - only the instance
    /// built by Register (where presence is actually being used) should set this true.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Presence options.
    /// </summary>
    /// <param name="presenceKey"></param>
    /// <param name="enabled"></param>
    public PresenceOptions(string presenceKey, bool enabled = false)
    {
        this.PresenceKey = presenceKey;
        this.Enabled = enabled;
    }
}

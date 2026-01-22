using Newtonsoft.Json;

namespace Supabase.Realtime.Broadcast;

/// <summary>
/// Options 
/// </summary>
public class BroadcastOptions
{
    /// <summary>
    /// self option enables client to receive message it broadcast
    /// </summary>
    [JsonProperty("self")]
    public bool BroadcastSelf { get; set; } = false;

    /// <summary>
    /// ack option instructs server to acknowledge that broadcast message was received
    /// </summary>
    [JsonProperty("ack")]
    public bool BroadcastAck { get; set; } = false;

    /// <summary>
    /// Initializes broadcast options
    /// </summary>
    /// <param name="broadcastSelf"></param>
    /// <param name="broadcastAck"></param>
    public BroadcastOptions(bool broadcastSelf = false, bool broadcastAck = false)
    {
        BroadcastSelf = broadcastSelf;
        BroadcastAck = broadcastAck;
    }
}
using Newtonsoft.Json;
using Supabase.Realtime.Models;

namespace Realtime.Tests.Models;

/// <summary>
///     A modeled broadcast payload used across the broadcast tests.
/// </summary>
public class BroadcastExample : BaseBroadcast
{
    [JsonProperty("userId")] public string? UserId { get; set; }
}

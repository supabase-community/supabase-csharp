using System.Text.Json.Serialization;
using Supabase.Realtime.Models;

namespace Realtime.Tests.Models;

/// <summary>
///     A modeled broadcast payload used across the broadcast tests.
/// </summary>
public class BroadcastExample : BaseBroadcast
{
    [JsonPropertyName("userId")] public string? UserId { get; set; }
}

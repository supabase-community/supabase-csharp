using System;
using System.Text.Json.Serialization;
using Supabase.Realtime.Models;

namespace Realtime.Tests.Models;

/// <summary>
///     A modeled presence payload used across the presence tests.
/// </summary>
public class PresenceExample : BasePresence
{
    [JsonPropertyName("time")] public DateTime? Time { get; set; }
}

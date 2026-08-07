using System;
using Newtonsoft.Json;
using Supabase.Realtime.Models;

namespace Realtime.Tests.Models;

/// <summary>
///     A modeled presence payload used across the presence tests.
/// </summary>
public class PresenceExample : BasePresence
{
    [JsonProperty("time")] public DateTime? Time { get; set; }
}

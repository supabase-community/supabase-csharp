using Newtonsoft.Json;

namespace Supabase.Realtime.Socket.Responses;

/// <summary>
/// A generic, internal phoenix server response
/// </summary>
public class PhoenixResponse
{
    /// <summary>
    /// The response.
    /// </summary>
    [JsonProperty("response")]
    public PostgresChangeResponse? Response;

    /// <summary>
    /// The status.
    /// </summary>
    [JsonProperty("status")]
    public string? Status;
}
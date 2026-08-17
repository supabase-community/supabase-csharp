using System.Text.Json.Serialization;

namespace Supabase.Realtime.Socket.Responses;

/// <summary>
/// A generic, internal phoenix server response
/// </summary>
public class PhoenixResponse
{
    /// <summary>
    /// The response.
    /// </summary>
    [JsonPropertyName("response")]
    public PostgresChangeResponse? Response;

    /// <summary>
    /// The status.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status;
}

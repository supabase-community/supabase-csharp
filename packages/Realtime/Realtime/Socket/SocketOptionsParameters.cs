using System.Text.Json.Serialization;

namespace Supabase.Realtime.Socket;

/// <summary>
/// Options to initialize a socket.
/// </summary>
public class SocketOptionsParameters
{
    /// <summary>
    /// A user token (used for WALRUS permissions)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// A Supabase hosted public key
    /// </summary>
    [JsonPropertyName("apikey")]
    public string? ApiKey { get; set; }
}

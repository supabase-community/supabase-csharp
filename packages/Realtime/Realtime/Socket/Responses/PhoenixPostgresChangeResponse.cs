using System.Text.Json.Serialization;

namespace Supabase.Realtime.Socket.Responses;

public class PhoenixPostgresChangeResponse
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("event")]
    public string? EventName { get; set; }

    [JsonPropertyName("filter")]
    public string? Filter { get; set; }

    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("table")]
    public string? Table { get; set; }
}

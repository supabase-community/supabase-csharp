using Newtonsoft.Json;

namespace Supabase.Realtime.Socket.Responses;

public class PhoenixPostgresChangeResponse
{
    [JsonProperty("id")]
    public int? id { get; set; }
    
    [JsonProperty("event")]
    public string? eventName { get; set; }
    
    [JsonProperty("filter")]
    public string? filter { get; set; }
    
    [JsonProperty("schema")]
    public string? schema { get; set; }
    
    [JsonProperty("table")]
    public string? table { get; set; }
}
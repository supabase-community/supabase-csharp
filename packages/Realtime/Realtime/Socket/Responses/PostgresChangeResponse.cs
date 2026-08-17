using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Realtime.Socket.Responses;

public class PostgresChangeResponse
{
    [JsonPropertyName("postgres_changes")]
    public List<PhoenixPostgresChangeResponse> Change { get; set; }
}

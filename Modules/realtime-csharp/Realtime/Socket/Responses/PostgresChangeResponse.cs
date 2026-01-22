using System.Collections.Generic;
using Newtonsoft.Json;

namespace Supabase.Realtime.Socket.Responses;

public class PostgresChangeResponse
{
   [JsonProperty("postgres_changes")] 
   public List<PhoenixPostgresChangeResponse> change { get; set; }
}
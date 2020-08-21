using System;
using Newtonsoft.Json;
using Supabase.Models;
using Supabase.Postgrest;

namespace SupabaseExample.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [JsonProperty("username")]
        public string Username { get; set; }
    }
}

using System;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Supabase.Models;

namespace SupabaseTests.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [JsonProperty("username")]
        public string Username { get; set; }
    }
}

using System;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using Supabase;

namespace SupabaseTests.Models
{
    [Table("users")]
    public class User : SupabaseModel
    {
        [JsonProperty("username")]
        public string Username { get; set; }
    }
}

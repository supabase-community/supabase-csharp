using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseExampleXA.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("status")]
        public string Status { get; set; }
    }
}

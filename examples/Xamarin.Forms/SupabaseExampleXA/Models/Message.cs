using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseExampleXA.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("inserted_at")]
        public DateTime InsertedAt { get; set; } = new DateTime();

        [Column("message")]
        public string Text { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("channel_id")]
        public int ChannelId { get; set; }
    }
}

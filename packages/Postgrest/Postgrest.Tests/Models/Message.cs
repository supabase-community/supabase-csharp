using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("username")]
        public string? UserName { get; set; }

        [Column("channel_id")]
        public int? ChannelId { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Message message &&
                   Id == message.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}

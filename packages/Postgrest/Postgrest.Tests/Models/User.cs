using System;
using System.Collections.Generic;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("username", true)]
        public string? Username { get; set; }

        [Column("data")]
        public string? Data { get; set; }

        [Column("favorite_numbers")]
        public List<int>? FavoriteNumbers { get; set; }

        [Column("favorite_name")]
        public string? FavoriteName { get; set; }

        [Column("age_range")]
        public IntRange? AgeRange { get; set; }

        [Column("catchphrase")]
        public string? Catchphrase { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("inserted_at")]
        public DateTime InsertedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is User user &&
                   Username == user.Username &&
                   Catchphrase == user.Catchphrase;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Username, Catchphrase);
        }
    }
}

using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RealtimeTests.Models;

[Table("todos")]
public class Todo : BaseModel
{
    [PrimaryKey("id")] public int Id { get; set; }

    [Column("details")] public string? Details { get; set; }

    [Column("user_id")] public int UserId { get; set; }

    [Column("numbers")] public List<int>? Numbers { get; set; } = new List<int>();

    [Column("inserted_at")] public DateTime? InsertedAt { get; set; }
}
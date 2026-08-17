using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Converters;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("todos")]
public class Todo : BaseModel
{
    [JsonConverter(typeof(PostgrestStringEnumConverter))]
    public enum TodoStatus
    {
        [EnumMember(Value = "NOT STARTED")]
        NOT_STARTED,
        [EnumMember(Value = "IN PROGRESS")]
        IN_PROGRESS,
        [EnumMember(Value = "DONE")]
        DONE,
    }

    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("status")]
    public TodoStatus Status { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("done")]
    public bool Done { get; set; }

    [Column("details")]
    public string? Details { get; set; }
}

using Postgrest.Attributes;

namespace BlazorWebAssemblySupabaseTemplate.Dtos;

[Table("TodoPrivate")]
public class TodoPrivate : BaseModelApp
{
    [Column("title")]
    public string? Title { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }
}

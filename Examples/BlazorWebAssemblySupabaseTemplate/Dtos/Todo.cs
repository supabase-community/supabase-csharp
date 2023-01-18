using Postgrest.Attributes;
using Postgrest.Models;

namespace BlazorWebAssemblySupabaseTemplate.Dtos;

[Table("Todo")]
public class Todo : BaseModelApp
{
    [Column("Title")]
    public string? Title { get; set; }
}

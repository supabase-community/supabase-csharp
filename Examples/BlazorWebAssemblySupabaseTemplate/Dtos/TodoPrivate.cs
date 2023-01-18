using System;
using Postgrest.Attributes;
using Postgrest.Models;
using Supabase;

namespace BlazorWebAssemblySupabaseTemplate.Dtos;

[Table("TodoPrivate")]
public class TodoPrivate : BaseModelApp
{
    [Column("title")]
    public string? Title { get; set; }

    [Column("user_id")]
    public string User_id { get; set; }
}

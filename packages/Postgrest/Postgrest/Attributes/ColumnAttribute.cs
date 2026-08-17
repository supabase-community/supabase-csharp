using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
namespace Supabase.Postgrest.Attributes;


/// <summary>
/// Used to map a C# property to a Postgrest Column.
/// </summary>
/// <example>
/// <code>
/// class User : BaseModel {
///     [ColumnName("firstName")]
///     public string FirstName {get; set;}
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    /// <summary>
    /// The name in postgres of this column.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Specifies what should be serialized in the event this column's value is NULL. Defaults to
    /// <see cref="JsonIgnoreCondition.Never"/> (the column is always written, matching the previous
    /// <c>NullValueHandling.Include</c>); use <see cref="JsonIgnoreCondition.WhenWritingNull"/> to omit it.
    /// </summary>
    public JsonIgnoreCondition NullValueHandling { get; set; }

    /// <summary>
    /// If the performed query is an Insert or Upsert, should this value be ignored?
    /// </summary>
    public bool IgnoreOnInsert { get; }

    /// <summary>
    /// If the performed query is an Update, should this value be ignored?
    /// </summary>
    public bool IgnoreOnUpdate { get; }

    /// <inheritdoc />
    public ColumnAttribute([CallerMemberName] string? columnName = null, JsonIgnoreCondition nullValueHandling = JsonIgnoreCondition.Never, bool ignoreOnInsert = false, bool ignoreOnUpdate = false)
    {
        this.ColumnName = columnName!; // Will either be user specified or given by runtime compiler.
        this.NullValueHandling = nullValueHandling;
        this.IgnoreOnInsert = ignoreOnInsert;
        this.IgnoreOnUpdate = ignoreOnUpdate;
    }
}

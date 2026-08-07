namespace Supabase.Realtime.PostgresChanges;

/// <summary>
/// Optional targeting for an <c>OnPostgresChange</c> listener — which schema, table, and rows to
/// listen to. The event to listen for is passed separately as a <see cref="PostgresChangesOptions.ListenType"/>
/// parameter; this object carries only the remaining, optional criteria (all with safe defaults).
/// </summary>
/// <example>
/// <code>
/// new PostgresChangesFilter { Table = "todos", Filter = "id=eq.1" }
/// </code>
/// </example>
public class PostgresChangesFilter
{
    /// <summary>
    /// The Postgres schema to listen to. Defaults to <c>public</c>.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// The table to listen to. <c>null</c> (the default) listens to every table in the schema.
    /// </summary>
    public string? Table { get; set; }

    /// <summary>
    /// An optional row filter in PostgREST syntax, e.g. <c>id=eq.1</c>.
    /// </summary>
    public string? Filter { get; set; }
}

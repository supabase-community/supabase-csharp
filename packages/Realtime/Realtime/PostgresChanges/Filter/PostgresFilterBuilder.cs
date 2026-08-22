using System;
using System.Collections.Generic;

namespace Supabase.Realtime.PostgresChanges.Filter;

/// <summary>
/// A builder for constructing filters for Postgres changes subscriptions.
/// This filter syntax is similar to PostgREST Supabase filters and allows
/// filtering realtime database events based on column values.
/// </summary>
public class PostgresFilterBuilder
{
    /// <summary>
    /// Internal list storing the individual filter expressions.
    /// </summary>
    private readonly List<string> filters = [];

    /// <summary>
    /// Private constructor to enforce the builder pattern.
    /// Use <see cref="Builder"/> to create instances.
    /// </summary>
    private PostgresFilterBuilder() { }

    /// <summary>
    /// Creates a new instance of the PostgresFilterBuilder.
    /// </summary>
    /// <returns>A new PostgresFilterBuilder instance.</returns>
    public static PostgresFilterBuilder Builder() => new();

    /// <summary>
    /// Adds a filter expression to the builder.
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="filterOperator">The filter operator to apply.</param>
    /// <param name="value">The value to compare against.</param>
    /// <param name="negate">Whether to negate the filter expression.</param>
    /// <exception cref="ArgumentException">Thrown when the column is null or empty.</exception>
    private void Add(
        string column,
        PostgresChangesFilterOperator filterOperator,
        object? value,
        bool negate = false
    )
    {
        if (string.IsNullOrEmpty(column))
        {
            throw new ArgumentException("Column cannot be null or empty.", nameof(column));
        }

        var prefix = negate ? "not." : "";
        var filterValue = new PostgresFilterValue(filterOperator, value);
        this.filters.Add(
            $"{column}={prefix}{filterOperator.ToMappedString()}.{filterValue.Value}"
        );
    }

    /// <summary>
    /// Adds an equality filter (column = value).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to compare for equality.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Eq(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Eq, value);
        return this;
    }

    /// <summary>
    /// Adds a not-equal filter (column != value).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to compare for inequality.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Neq(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Neq, value);
        return this;
    }

    /// <summary>
    /// Adds a less-than filter (column &lt; value).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Lt(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Lt, value);
        return this;
    }

    /// <summary>
    /// Adds a less-than-or-equal filter (column &lt;= value).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Lte(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Lte, value);
        return this;
    }

    /// <summary>
    /// Adds a greater-than filter (column &gt; value).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Gt(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Gt, value);
        return this;
    }

    /// <summary>
    /// Adds a greater-than-or-equal filter (column &gt;= value).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Gte(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Gte, value);
        return this;
    }

    /// <summary>
    /// Adds an in-list filter to check if column value is in the provided list.
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The list of values to check against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder In(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.In, value);
        return this;
    }

    /// <summary>
    /// Adds a case-sensitive pattern matching filter (LIKE).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The pattern to match (supports % and _ wildcards).</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Like(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Like, value);
        return this;
    }

    /// <summary>
    /// Adds a case-insensitive pattern matching filter (ILIKE).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The pattern to match (supports % and _ wildcards).</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder ILike(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.ILike, value);
        return this;
    }

    /// <summary>
    /// Adds a case-sensitive regular expression match filter.
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The regular expression pattern to match.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Match(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Match, value);
        return this;
    }

    /// <summary>
    /// Adds a case-insensitive regular expression match filter.
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The regular expression pattern to match.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder IMatch(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.IMatch, value);
        return this;
    }

    /// <summary>
    /// Adds a filter to check if column value is distinct from the provided value.
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to check distinctness against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder IsDistinct(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.IsDistinct, value);
        return this;
    }

    /// <summary>
    /// Adds a filter to check if column IS the provided value (null, true, or false).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="value">The value to check (typically null, true, or false).</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Is(string column, object? value)
    {
        this.Add(column, PostgresChangesFilterOperator.Is, value);
        return this;
    }

    /// <summary>
    /// Adds a negated filter expression (NOT operator).
    /// </summary>
    /// <param name="column">The column name to filter on.</param>
    /// <param name="op">The filter operator to negate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public PostgresFilterBuilder Not(string column, PostgresChangesFilterOperator op, object? value)
    {
        this.Add(column, op, value, true);
        return this;
    }

    /// <summary>
    /// Builds and returns the final filter string by joining all filter expressions with commas.
    /// </summary>
    /// <returns>A comma-separated string of all filter expressions.</returns>
    public string Build() => string.Join(",", this.filters);
}

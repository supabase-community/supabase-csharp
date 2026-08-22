using System.Linq;
using System.Reflection;
using Supabase.Core.Attributes;

namespace Supabase.Realtime.PostgresChanges.Filter;

/// <summary>
///     Filter operators for PostgreSQL realtime changes. These operators are used in
///     <see cref="PostgresChangesFilter"/> to specify comparison conditions on column values
///     when subscribing to database changes via realtime channels.
/// </summary>
public enum PostgresChangesFilterOperator
{
    /// <summary>Equal to.</summary>
    [MapTo("eq")]
    Eq,

    /// <summary>Not equal to.</summary>
    [MapTo("neq")]
    Neq,

    /// <summary>Less than.</summary>
    [MapTo("lt")]
    Lt,

    /// <summary>Less than or equal to.</summary>
    [MapTo("lte")]
    Lte,

    /// <summary>Greater than.</summary>
    [MapTo("gt")]
    Gt,

    /// <summary>Greater than or equal to.</summary>
    [MapTo("gte")]
    Gte,

    /// <summary>Value is in a set.</summary>
    [MapTo("in")]
    In,

    /// <summary>Case-sensitive pattern matching.</summary>
    [MapTo("like")]
    Like,

    /// <summary>Case-insensitive pattern matching.</summary>
    [MapTo("ilike")]
    ILike,

    /// <summary>Exact match (for null/boolean).</summary>
    [MapTo("is")]
    Is,

    /// <summary>Case-sensitive regular expression match.</summary>
    [MapTo("match")]
    Match,

    /// <summary>Case-insensitive regular expression match.</summary>
    [MapTo("imatch")]
    IMatch,

    /// <summary>Value is distinct from.</summary>
    [MapTo("isdistinct")]
    IsDistinct,
}

/// <summary>
///     Extension methods for <see cref="PostgresChangesFilterOperator"/>.
/// </summary>
public static class PostgresChangesFilterOperatorExtensions
{
    /// <summary>
    ///     Converts the <see cref="PostgresChangesFilterOperator"/> enum value to its protocol string
    ///     representation using the <see cref="MapToAttribute"/> mapping.
    /// </summary>
    /// <param name="filterOperator">The filter operator to convert.</param>
    /// <returns>The mapped string value (e.g., "eq", "neq") or the enum name if no mapping exists.</returns>
    public static string ToMappedString(this PostgresChangesFilterOperator filterOperator)
    {
        var member = typeof(PostgresChangesFilterOperator)
            .GetMember(filterOperator.ToString())
            .FirstOrDefault();

        var attribute = member?.GetCustomAttribute<MapToAttribute>();

        return attribute?.Mapping ?? filterOperator.ToString();
    }
}

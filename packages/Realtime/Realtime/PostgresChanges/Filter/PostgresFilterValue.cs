using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Supabase.Realtime.PostgresChanges.Filter;

/// <summary>
///     Represents a serialized filter value used in <see cref="PostgresFilterBuilder" />.
///     Handles the conversion and sanitization of filter values based on the specified operator,
///     ensuring they are properly formatted for PostgreSQL change subscriptions.
/// </summary>
public class PostgresFilterValue
{
    /// <summary>
    ///     Gets the serialized filter value as a string, ready to be used in a PostgreSQL changes filter.
    /// </summary>
    internal string? Value { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgresFilterValue" /> class.
    ///     Serializes the provided value according to the specified filter operator.
    /// </summary>
    /// <param name="operation">The filter operator that determines how the value should be serialized.</param>
    /// <param name="value">The value to serialize, which may be null, a scalar, or a collection depending on the operator.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported operator is specified.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the value type is incompatible with the specified operator
    ///     (e.g., non-enumerable for 'in' operator, or invalid value for 'is' operator).
    /// </exception>
    public PostgresFilterValue(PostgresChangesFilterOperator operation, object? value)
    {
        var sanitized = operation switch
        {
            PostgresChangesFilterOperator.Eq
            or PostgresChangesFilterOperator.Neq
            or PostgresChangesFilterOperator.Lt
            or PostgresChangesFilterOperator.Lte
            or PostgresChangesFilterOperator.Gt
            or PostgresChangesFilterOperator.Gte
            or PostgresChangesFilterOperator.Match
            or PostgresChangesFilterOperator.IMatch
            or PostgresChangesFilterOperator.Like
            or PostgresChangesFilterOperator.ILike
            or PostgresChangesFilterOperator.IsDistinct => SerializeScalar(value),

            PostgresChangesFilterOperator.In => SerializeIn(value),

            PostgresChangesFilterOperator.Is => SerializeIsValue(value),

            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };

        this.Value = sanitized;
    }

    /// <summary>
    ///     Determines whether a string value needs to be quoted.
    ///     Quoting is required for null/empty strings or strings containing special characters
    ///     such as whitespace, punctuation, or delimiter characters.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <returns><c>true</c> if the value needs quoting; otherwise, <c>false</c>.</returns>
    private static bool NeedQuoting(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return value.Any(character =>
            char.IsWhiteSpace(character)
            || character is ',' or '.' or ':' or '(' or ')' or '"' or '\\'
        );
    }

    /// <summary>
    ///     Quotes and escapes a string value for safe transmission.
    ///     Backslashes and double quotes are escaped, and the result is wrapped in double quotes.
    /// </summary>
    /// <param name="value">The string value to quote.</param>
    /// <returns>The quoted and escaped string.</returns>
    private static string Quote(string value)
    {
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    /// <summary>
    ///     Serializes a string value, quoting it if necessary.
    /// </summary>
    /// <param name="value">The string value to serialize.</param>
    /// <returns>The serialized string, quoted if it contains special characters.</returns>
    private static string SerializeString(string value) =>
        NeedQuoting(value) ? Quote(value) : value;

    /// <summary>
    ///     Serializes a scalar value (null, boolean, number, or string) to its string representation.
    ///     Handles type-specific formatting: null becomes "null", booleans are lowercased,
    ///     decimals use invariant culture, and other types are converted to strings and quoted if needed.
    /// </summary>
    /// <param name="value">The scalar value to serialize.</param>
    /// <returns>The serialized string representation of the value.</returns>
    private static string SerializeScalar(object? value) =>
        value switch
        {
            null => "null",
            bool parsed => parsed.ToString().ToLowerInvariant(),
            decimal => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
            string parsed => SerializeString(parsed),
            _ => SerializeString(value.ToString() ?? "null"),
        };

    /// <summary>
    ///     Serializes a value for the 'IS' operator, which only accepts null, true, or false.
    ///     String inputs are checked case-insensitively for these special values.
    /// </summary>
    /// <param name="value">The value to serialize for the IS operator.</param>
    /// <returns>The serialized value: "null", "true", or "false".</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not null, true, or false.</exception>
    private static string SerializeIsValue(object? value) =>
        value switch
        {
            null => "null",
            bool parsed => parsed.ToString().ToLowerInvariant(),
            string parsed when parsed.Equals("null", StringComparison.OrdinalIgnoreCase) => "null",
            string parsed when parsed.Equals("true", StringComparison.OrdinalIgnoreCase) => "true",
            string parsed when parsed.Equals("false", StringComparison.OrdinalIgnoreCase) =>
                "false",
            _ => throw new ArgumentException(
                "The 'is' operator only supports null, true, false, or unknown values.",
                nameof(value)
            ),
        };

    /// <summary>
    ///     Serializes a collection of values for the 'IN' operator.
    ///     The collection is converted to a comma-separated list enclosed in parentheses,
    ///     with each value serialized as a scalar.
    /// </summary>
    /// <param name="value">The enumerable collection of values to serialize.</param>
    /// <returns>A parenthesized, comma-separated list of serialized values.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when the value is not an IEnumerable (excluding string) or when the collection is empty.
    /// </exception>
    private static string SerializeIn(object? value)
    {
        if (value is string || value is not IEnumerable values)
            throw new ArgumentException(
                "The 'in' operator only supports IEnumerable values.",
                nameof(value)
            );

        var items = values.Cast<object?>().ToList();
        if (items.Count == 0)
            throw new ArgumentException(
                "The 'in' operator requires at least one value.",
                nameof(value)
            );

        var serialized = items.Select(SerializeScalar);

        return $"({string.Join(",", serialized)})";
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Postgrest.Converters;

/// <summary>
/// ISO 8601 format Postgrest emits and accepts, matching the previous Newtonsoft output
/// (<c>yyyy-MM-ddTHH:mm:ss.FFFFFFFK</c>): fractional seconds only when non-zero, and an offset
/// suffix only for a non-<see cref="DateTimeKind.Unspecified"/> value.
/// </summary>
internal static class DateTimeFormats
{
    internal const string Iso8601 = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";
}

/// <inheritdoc />
public class DateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadDateTime(reader.GetString()) ?? default;

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        WriteDateTime(writer, value);

    /// <summary>
    /// Returns the parsed value, keeping its <see cref="DateTimeKind"/> and sub-second precision intact
    /// rather than round-tripping it through a culture-formatted string (which dropped the offset and
    /// fractional seconds). The `infinity` sentinels are still mapped to <see cref="DateTime.MaxValue"/> /
    /// <see cref="DateTime.MinValue"/>.
    /// </summary>
    internal static DateTime? ReadDateTime(string? value) =>
        value switch
        {
            null => null,
            _ => ParseInfinity(value) ?? DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };

    private static DateTime? ParseInfinity(string input) =>
        input.Contains("infinity") ? input.Contains("-") ? DateTime.MinValue : DateTime.MaxValue : null;

    /// <summary>
    /// Writes a single value with its wall-clock intact, mapping <see cref="DateTime.MaxValue"/> back to
    /// the `infinity` sentinel the read path maps it from: Postgres rounds `MaxValue` up to year 10000
    /// when stored as a literal timestamp, which then cannot be read back. <see cref="DateTime.MinValue"/>
    /// is left as a literal (it round-trips cleanly and doubles as the default for an unset value, so it
    /// is not `-infinity`). Mirroring the read path keeps an <see cref="DateTimeKind.Unspecified"/> `date`
    /// from being shifted to the previous day in timezones ahead of UTC.
    /// </summary>
    internal static void WriteDateTime(Utf8JsonWriter writer, DateTime value)
    {
        if (value == DateTime.MaxValue)
            writer.WriteStringValue("infinity");
        else
            writer.WriteStringValue(value.ToString(DateTimeFormats.Iso8601, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Applies <see cref="DateTimeConverter" />'s single-value handling across a list, matching the previous
/// Newtonsoft converter which handled both <see cref="DateTime" /> and <c>List&lt;DateTime&gt;</c>.
/// </summary>
public class DateTimeListConverter : JsonConverter<List<DateTime>>
{
    /// <inheritdoc />
    public override List<DateTime>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            return null;

        var list = new List<DateTime>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var date = DateTimeConverter.ReadDateTime(reader.GetString());
            if (date != null)
                list.Add(date.Value);
        }

        return list;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, List<DateTime> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var date in value)
            DateTimeConverter.WriteDateTime(writer, date);
        writer.WriteEndArray();
    }
}

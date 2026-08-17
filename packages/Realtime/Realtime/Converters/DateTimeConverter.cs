using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Realtime.Converters;

/// <summary>
/// Reads the timestamp strings Postgres emits (mapping the `infinity` sentinels to
/// <see cref="DateTime.MaxValue"/> / <see cref="DateTime.MinValue"/>) and writes them back with the
/// configured format, matching the previous read-side custom converter plus write-side IsoDateTimeConverter.
/// </summary>
internal class DateTimeConverter : JsonConverter<DateTime>
{
    private readonly string format;

    internal DateTimeConverter(string format) => this.format = format;

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadDateTime(reader.GetString()) ?? default;

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime().ToString(this.format, CultureInfo.InvariantCulture));

    internal static DateTime? ReadDateTime(string? value)
    {
        if (value == null)
            return null;

        return ParseInfinity(value) ?? DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTime? ParseInfinity(string input) =>
        input.Contains("infinity") ? input.Contains("-") ? DateTime.MinValue : DateTime.MaxValue : null;
}

/// <summary>
/// Applies <see cref="DateTimeConverter" />'s single-value handling across a list, matching the previous
/// read-side converter which handled both <see cref="DateTime" /> and <c>List&lt;DateTime&gt;</c>.
/// </summary>
internal class DateTimeListConverter : JsonConverter<List<DateTime>>
{
    private readonly string format;

    internal DateTimeListConverter(string format) => this.format = format;

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

    public override void Write(Utf8JsonWriter writer, List<DateTime> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var date in value)
            writer.WriteStringValue(date.ToUniversalTime().ToString(this.format, CultureInfo.InvariantCulture));
        writer.WriteEndArray();
    }
}

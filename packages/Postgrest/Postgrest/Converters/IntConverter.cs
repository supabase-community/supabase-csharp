using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Postgrest.Converters;

/// <summary>
/// Reads a <c>List&lt;int&gt;</c> from the JSON array Postgrest returns (e.g. <c>[1,2,3]</c>) or from a
/// Postgres array literal (<c>"{1,2,3}"</c>). Writes a plain JSON array, which Postgrest accepts for
/// array columns and which keeps the output independent of the current culture.
/// </summary>
public class IntArrayConverter : JsonConverter<List<int>>
{
    /// <inheritdoc />
    public override List<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                var literal = reader.GetString()!;
                var contents = literal.Trim('{', '}');
                if (contents.Length == 0)
                    return new List<int>();
                var result = new List<int>();
                foreach (var part in contents.Split(','))
                {
                    if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var item))
                        throw new JsonException($"Cannot read '{literal}' as List<int>.");
                    result.Add(item);
                }
                return result;
            case JsonTokenType.StartArray:
                var list = new List<int>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(reader.GetInt32());
                return list;
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading List<int>.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
            writer.WriteNumberValue(item);
        writer.WriteEndArray();
    }
}

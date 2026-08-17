using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Postgrest.Converters;

/// <summary>
/// Serializes a <c>List&lt;int&gt;</c> as the Postgres array literal (e.g. <c>{1,2,3}</c>) Postgrest
/// expects. Write-only, matching the previous Newtonsoft converter.
/// </summary>
public class IntArrayConverter : JsonConverter<List<int>>
{
    /// <summary>
    /// Reads the JSON array Postgrest returns (e.g. <c>[1,2,3]</c>) — the previous Newtonsoft converter
    /// was write-only (<c>CanRead = false</c>) and let the default reader handle this shape, so the array
    /// form is preserved. The Postgres literal string form (<c>"{1,2,3}"</c>) is also accepted.
    /// </summary>
    public override List<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                var literal = reader.GetString()!.Trim('{', '}');
                if (string.IsNullOrEmpty(literal))
                    return new List<int>();
                var result = new List<int>();
                foreach (var part in literal.Split(','))
                    result.Add(int.Parse(part));
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
    public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"{{{string.Join(",", value)}}}");
}

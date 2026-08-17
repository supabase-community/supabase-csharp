using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("Realtime.Tests")]

namespace Supabase.Realtime.Converters;

/// <summary>
/// A string array converter that specifically parses Postgrest styled arrays `{big,string,array}` and
/// `[1,2,3]` from strings into a <see cref="List{T}"/>. A regular JSON array is also accepted; writes emit a
/// regular JSON array.
/// </summary>
public class StringArrayConverter : JsonConverter<List<string>>
{
    /// <inheritdoc />
    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    return Parse(reader.GetString()!);
                case JsonTokenType.StartArray:
                    var list = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        list.Add(reader.GetString()!);
                    return list;
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }

    internal static List<string> Parse(string value)
    {
        var result = new List<string>();

        if (string.IsNullOrEmpty(value))
            return result;

        var firstChar = value[0];
        var lastChar = value[value.Length - 1];

        var isBraced = (firstChar == '{' && lastChar == '}') || (firstChar == '[' && lastChar == ']');
        if (!isBraced)
            return result;

        foreach (var item in value.Trim('{', '}', '[', ']').Split(','))
        {
            if (string.IsNullOrEmpty(item)) continue;
            result.Add(item);
        }

        return result;
    }
}

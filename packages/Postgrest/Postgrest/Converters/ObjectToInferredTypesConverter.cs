using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Postgrest.Converters;

/// <summary>
/// Deserializes JSON values typed as <see cref="object"/> — for example the values of the
/// <c>Dictionary&lt;string, object&gt;</c> the request body is re-parsed into before it is sent — into
/// their natural CLR types (string, long, double, bool, nested dictionary, or list) instead of leaving
/// them as <see cref="JsonElement"/>. Date/time values are deliberately left as strings (matching the
/// previous <c>DateParseHandling.None</c>), so the column converters' formatting passes through verbatim.
/// </summary>
internal sealed class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var l) ? l : reader.GetDouble();
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options);
            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(this.Read(ref reader, typeof(object), options));
                return list;
            default:
                using (var document = JsonDocument.ParseValue(ref reader))
                    return document.RootElement.Clone();
        }
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();
        if (runtimeType == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        JsonSerializer.Serialize(writer, value, runtimeType, options);
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Storage.Serialization;

/// <summary>
/// Deserializes JSON values typed as <see cref="object"/> — for example the values of a
/// <c>Dictionary&lt;string, object&gt;</c> such as a <see cref="FileObject"/>'s metadata — into
/// their natural CLR types (string, long, double, bool, nested dictionary, or list) instead of
/// leaving them as <see cref="JsonElement"/>. This matches how Newtonsoft.Json populated these
/// members, so consumers can keep reading metadata values by direct cast
/// (e.g. <c>(long) fileObject.MetaData["size"]</c>). Serialization is delegated to each value's
/// runtime type, which preserves the default output.
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

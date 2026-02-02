using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("RealtimeTests")]

namespace Supabase.Realtime.Converters;

/// <summary>
/// An string array converter that specifically parses Postgrest styled arrays `{big,string,array}` and `[1,2,3]`
/// from strings into a <see cref="List{T}"/>.
/// </summary>
public class StringArrayConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => throw new NotImplementedException();

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        try
        {
            if (reader.Value != null)
                return Parse((string)reader.Value);

            var jo = JArray.Load(reader);
            return jo.ToObject<List<string>>(serializer);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    internal static List<string> Parse(string value)
    {
        var result = new List<string>();

        var firstChar = value[0];
        var lastChar = value[value.Length - 1];

        switch (firstChar)
        {
            // {1,2,3}
            case '{' when lastChar == '}':
            {
                var array = value.Trim(new char[] { '{', '}' }).Split(',');
                foreach (var item in array)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    result.Add(item);
                }

                return result;
            }
            // [1,2,3]
            case '[' when lastChar == ']':
            {
                var array = value.Trim(new char[] { '[', ']' }).Split(',');
                foreach (var item in array)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    result.Add(item);
                }

                return result;
            }
            default:
                return result;
        }
    }
}
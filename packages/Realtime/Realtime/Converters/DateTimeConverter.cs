using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Supabase.Realtime.Converters;

internal class DateTimeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        throw new NotImplementedException();
    }

    public override bool CanWrite => false;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.Value != null)
        {
            var str = reader.Value.ToString();

            var infinity = ParseInfinity(str);

            if (infinity != null)
            {
                return (DateTime)infinity;
            }

            var date = DateTime.Parse(str);
            return date;
        }

        var result = new List<DateTime>();

        try
        {
            var jo = JArray.Load(reader);

            foreach (var item in jo.ToArray())
            {
                var inner = item.ToString();

                var infinity = ParseInfinity(inner);

                if (infinity != null)
                {
                    result.Add((DateTime)infinity);
                }

                var date = DateTime.Parse(inner);
                result.Add(date);
            }
        }
        catch (JsonReaderException)
        {
            return null;
        }


        return result;
    }

    private static DateTime? ParseInfinity(string input)
    {
        if (input.Contains("infinity"))
        {
            return input.Contains("-") ? DateTime.MinValue : DateTime.MaxValue;
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
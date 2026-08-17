using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Extensions;

namespace Supabase.Postgrest.Converters;

/// <summary>
/// Used by System.Text.Json to convert a C# range into a Postgrest range.
/// </summary>
internal sealed class RangeConverter : JsonConverter<IntRange>
{
    public override IntRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value != null ? ParseIntRange(value) : null;
    }

    public override void Write(Utf8JsonWriter writer, IntRange value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToPostgresString());
    }

    public static IntRange ParseIntRange(string value)
    {
        //int4range (0,1] , [123,4123], etc. etc.
        const string pattern = @"^(\[|\()(\d+),(\d+)(\]|\))$";
        var matches = Regex.Matches(value, pattern);

        if (matches.Count <= 0)
            throw new PostgrestException("Unknown Range format.") { Reason = FailureHint.Reason.InvalidArgument };

        var groups = matches[0].Groups;
        var isInclusiveLower = groups[1].Value == "[";
        var isInclusiveUpper = groups[4].Value == "]";
        var value1 = int.Parse(groups[2].Value);
        var value2 = int.Parse(groups[3].Value);

        var start = isInclusiveLower ? value1 : value1 + 1;
        var count = isInclusiveUpper ? value2 : value2 - 1;

        // Edge-case, includes no points
        return count < start ? new IntRange(0, 0) : new IntRange(start, count);
    }
}

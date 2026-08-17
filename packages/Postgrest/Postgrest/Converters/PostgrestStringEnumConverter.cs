using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Supabase.Postgrest.Converters;

/// <summary>
/// The enum converter Postgrest registers for every enum. On read it accepts both the string form
/// (honoring <see cref="EnumMemberAttribute" />, as Postgres returns enum columns as text) and the
/// numeric underlying value, matching Newtonsoft's lenient enum reading — System.Text.Json's built-in
/// converter reads only numbers and would throw on the string. On write it emits the numeric value, or
/// the string when <see cref="ClientOptions.SerializeEnumsAsStrings" /> is enabled.
/// </summary>
internal sealed class PostgrestEnumConverter : JsonConverterFactory
{
    private readonly bool writeAsString;

    internal PostgrestEnumConverter(bool writeAsString) => this.writeAsString = writeAsString;

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // System.Text.Json consults options.Converters before a type-level [JsonConverter], so this
        // global converter would otherwise shadow a per-enum PostgrestStringEnumConverter. Honor that
        // attribute here so an enum that opts into the string form always gets it, as Newtonsoft did.
        var forceString = this.writeAsString ||
            typeToConvert.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType == typeof(PostgrestStringEnumConverter);

        return (JsonConverter) Activator.CreateInstance(
            typeof(EnumMemberConverter<>).MakeGenericType(typeToConvert), new object[] { forceString })!;
    }
}

/// <summary>
/// Serializes an enum as a string, honoring <see cref="EnumMemberAttribute" /> the way Newtonsoft's
/// <c>StringEnumConverter</c> did — System.Text.Json's built-in <see cref="JsonStringEnumConverter" />
/// ignores it and would emit the member name. Apply it per-enum with
/// <c>[JsonConverter(typeof(PostgrestStringEnumConverter))]</c> to force the string form regardless of
/// <see cref="ClientOptions.SerializeEnumsAsStrings" />.
/// </summary>
public sealed class PostgrestStringEnumConverter : JsonConverterFactory
{
    /// <inheritdoc />
    // Only the bare enum: System.Text.Json's built-in nullable machinery wraps this converter for
    // Nullable&lt;TEnum&gt; members, so claiming the nullable type here would double-handle it and throw.
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter) Activator.CreateInstance(
            typeof(EnumMemberConverter<>).MakeGenericType(typeToConvert), new object[] { true })!;
}

internal sealed class EnumMemberConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private readonly bool writeAsString;
    private readonly Dictionary<T, string> toWire = new();
    private readonly Dictionary<string, T> fromWire = new(StringComparer.OrdinalIgnoreCase);

    public EnumMemberConverter(bool writeAsString)
    {
        this.writeAsString = writeAsString;
        foreach (var name in Enum.GetNames(typeof(T)))
        {
            var value = (T) Enum.Parse(typeof(T), name);
            var member = typeof(T).GetField(name)?.GetCustomAttribute<EnumMemberAttribute>();
            var wire = member?.Value ?? name;
            this.toWire[value] = wire;
            this.fromWire[wire] = value;
        }
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return (T) Enum.ToObject(typeof(T), reader.GetInt64());

        var text = reader.GetString();
        if (text != null && this.fromWire.TryGetValue(text, out var value))
            return value;

        return text != null && Enum.TryParse<T>(text, true, out var parsed) ? parsed : default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (this.writeAsString)
            writer.WriteStringValue(this.toWire.TryGetValue(value, out var wire) ? wire : value.ToString());
        else
            writer.WriteNumberValue(Convert.ToInt64(value));
    }
}

using System;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Converters;

namespace Supabase.Postgrest;

/// <summary>
/// The write operation a model is being serialized for. Mirrors the state the previous
/// <c>PostgrestContractResolver</c> toggled, but is baked into a dedicated
/// <see cref="JsonSerializerOptions" /> per operation because System.Text.Json freezes options (and
/// caches the per-type contract) on first use, so a single mutable resolver cannot be reused.
/// </summary>
internal enum PostgrestOperation
{
    /// <summary>Read, RPC, and any serialization that drops no columns (the default).</summary>
    None = 0,
    Insert = 1,
    Update = 2,
    Upsert = 3,
}

/// <summary>
/// Builds the <see cref="JsonSerializerOptions" /> that replace the Newtonsoft
/// <c>JsonSerializerSettings</c> + <c>PostgrestContractResolver</c>. The custom column converters are
/// registered globally (System.Text.Json auto-wraps them for nullable members), and a
/// <see cref="DefaultJsonTypeInfoResolver" /> modifier applies the column-name mapping, per-column null
/// handling and per-operation column dropping the resolver used to do in <c>CreateProperty</c>.
/// </summary>
internal static class PostgrestSerializerOptions
{
    internal static JsonSerializerOptions Build(bool serializeEnumsAsStrings, PostgrestOperation operation)
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo => Modify(typeInfo, operation));

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            PropertyNameCaseInsensitive = true,
            // PostgREST returns some numeric columns as JSON strings (e.g. "user_id":"1"); Newtonsoft coerced
            // these into the model's numeric property, System.Text.Json is strict without this.
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new DateTimeConverter(),
                new DateTimeListConverter(),
                new IntArrayConverter(),
                new RangeConverter(),
                // Registered for every enum so reads accept both the string form Postgres returns and the
                // numeric form (Newtonsoft parity); the write form follows SerializeEnumsAsStrings. A
                // per-enum [JsonConverter(typeof(PostgrestStringEnumConverter))] still wins where present.
                new PostgrestEnumConverter(serializeEnumsAsStrings),
                new ObjectToInferredTypesConverter(),
            },
        };

        return options;
    }

    /// <summary>
    /// Re-parses an already-serialized payload into the loose object graph sent on the wire, keeping
    /// date/time values as their serialized strings (the previous <c>DateParseHandling.None</c>) so the
    /// column converters' formatting passes through verbatim.
    /// </summary>
    internal static readonly JsonSerializerOptions Passthrough = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new ObjectToInferredTypesConverter() },
    };

    private static void Modify(JsonTypeInfo typeInfo, PostgrestOperation operation)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var property in typeInfo.Properties)
        {
            if (property.AttributeProvider is not MemberInfo member)
                continue;

            if (!member.IsDefined(typeof(ColumnAttribute), true) &&
                !member.IsDefined(typeof(ReferenceAttribute), true) &&
                !member.IsDefined(typeof(PrimaryKeyAttribute), true))
                continue;

            var columnAttribute = member.GetCustomAttribute<ColumnAttribute>();
            if (columnAttribute != null)
            {
                property.Name = columnAttribute.ColumnName;

                var ignoredByOperation =
                    (operation == PostgrestOperation.Insert && columnAttribute.IgnoreOnInsert) ||
                    (operation == PostgrestOperation.Update && columnAttribute.IgnoreOnUpdate) ||
                    (operation == PostgrestOperation.Upsert &&
                     (columnAttribute.IgnoreOnUpdate || columnAttribute.IgnoreOnInsert));

                property.ShouldSerialize = ignoredByOperation
                    ? static (_, _) => false
                    : ShouldSerializeForNullHandling(columnAttribute.NullValueHandling);

                continue;
            }

            var referenceAttribute = member.GetCustomAttribute<ReferenceAttribute>();
            if (referenceAttribute != null)
            {
                // If a foreign key is not specified, PostgREST returns JSON keyed by the table's name.
                property.Name = (string.IsNullOrEmpty(referenceAttribute.ForeignKey)
                    ? referenceAttribute.TableName
                    : referenceAttribute.ColumnName) ?? property.Name;

                if (operation == PostgrestOperation.Insert || operation == PostgrestOperation.Update)
                    property.ShouldSerialize = static (_, _) => false;

                continue;
            }

            var primaryKeyAttribute = member.GetCustomAttribute<PrimaryKeyAttribute>();
            if (primaryKeyAttribute != null)
            {
                property.Name = primaryKeyAttribute.ColumnName;
                var shouldInsert = primaryKeyAttribute.ShouldInsert;
                var isUpsert = operation == PostgrestOperation.Upsert;
                property.ShouldSerialize = (instance, _) => shouldInsert || (isUpsert && instance != null);
            }
        }
    }

    private static Func<object, object?, bool>? ShouldSerializeForNullHandling(JsonIgnoreCondition condition) =>
        condition switch
        {
            JsonIgnoreCondition.WhenWritingNull => static (_, value) => value != null,
            JsonIgnoreCondition.WhenWritingDefault => static (_, value) => value != null,
            JsonIgnoreCondition.Always => static (_, _) => false,
            _ => null, // Never / unset — serialize as-is, matching Newtonsoft's NullValueHandling.Include
        };
}

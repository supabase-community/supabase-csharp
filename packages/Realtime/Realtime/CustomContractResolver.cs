using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Realtime.Converters;

namespace Supabase.Realtime;

/// <summary>
/// A custom resolver that handles mapping column names and property names as well
/// as handling the conversion of Postgrest Ranges to a C# `Range`.
/// </summary>
internal class CustomContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);

        if (prop.PropertyType == typeof(List<int>))
        {
            prop.Converter = new IntArrayConverter();
        }
        else if (prop.PropertyType == typeof(List<string>))
        {
            prop.Converter = new StringArrayConverter();
        }
        else if (prop.PropertyType == typeof(DateTime) || Nullable.GetUnderlyingType(prop.PropertyType!) == typeof(DateTime))
        {
            prop.Converter = new DateTimeConverter();
        }
        else if (prop.PropertyType == typeof(List<DateTime>) || Nullable.GetUnderlyingType(prop.PropertyType!) == typeof(List<DateTime>))
        {
            prop.Converter = new DateTimeConverter();
        }

        // Dynamically set the name of the key we are serializing/deserializing from the model.
        if (member.CustomAttributes.Any())
        {
            var columnAtt = member.GetCustomAttribute<ColumnAttribute>();

            if (columnAtt != null)
            {
                prop.PropertyName = columnAtt.ColumnName;
                // Postgrest's ColumnAttribute now carries System.Text.Json's JsonIgnoreCondition; map it back
                // to Newtonsoft's NullValueHandling here since Realtime still serializes with Newtonsoft.
                prop.NullValueHandling = columnAtt.NullValueHandling == JsonIgnoreCondition.WhenWritingNull
                    ? NullValueHandling.Ignore
                    : NullValueHandling.Include;
                return prop;
            }

            var primaryKeyAtt = member.GetCustomAttribute<PrimaryKeyAttribute>();

            if (primaryKeyAtt != null)
            {
                prop.PropertyName = primaryKeyAtt.ColumnName;
                prop.ShouldSerialize = _ => primaryKeyAtt.ShouldInsert;
                return prop;
            }
        }

        return prop;
    }
}

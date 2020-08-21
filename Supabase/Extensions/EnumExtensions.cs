using System;
using System.Reflection;
using Supabase.Postgrest;

namespace Supabase.Extensions
{
    public static class EnumExtensions
    {
        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(T)) as T;
                    if (attr != null)
                    {
                        return attr;
                    }
                }
            }
            return null;
        }
    }
}

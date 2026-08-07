using Supabase.Core.Attributes;
using System;
using System.Linq;

namespace Supabase.Core
{
    /// <summary>
    /// Shortcut Methods, mostly focused on getting attributes from class properties and enums.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Returns the current value from a given class property
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetPropertyValue<T>(object obj, string propName) => (T)obj.GetType().GetProperty(propName).GetValue(obj, null);

        /// <summary>
        /// Returns a cast Custom Attribute from a given object.
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetCustomAttribute<T>(object obj) where T : Attribute => (T)Attribute.GetCustomAttribute(obj.GetType(), typeof(T));

        /// <summary>
        /// Returns a cast Custom Attribute from a given type.
        /// </summary>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetCustomAttribute<T>(Type type) where T : Attribute => (T)Attribute.GetCustomAttribute(type, typeof(T));

        /// <summary>
        /// Shortcut method for accessing a `MapTo` attribute, combined with an Enum.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static MapToAttribute GetMappedToAttr(Enum obj)
        {
            var type = obj.GetType();
            var name = Enum.GetName(type, obj);
            return type.GetField(name).GetCustomAttributes(false).OfType<MapToAttribute>().SingleOrDefault();
        }
    }
}

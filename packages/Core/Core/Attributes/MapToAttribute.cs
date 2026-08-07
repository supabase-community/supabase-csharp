using System;

namespace Supabase.Core.Attributes
{
    /// <summary>
    /// Used internally to add a string value to a C# field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MapToAttribute : Attribute
    {
        /// <summary>
        /// The externally specified target value.
        /// </summary>
        public string Mapping { get; set; }

        /// <summary>
        /// A formatter to be passed into the <see cref="String.ToString()" /> method.
        /// </summary>
        public string? Formatter { get; set; }

        /// <summary>
        /// Creates a Mapping to be used internally.
        ///
        /// For example, specifying an Enum that has a different string value elsewhere.
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="formatter"></param>
        public MapToAttribute(string mapping, string? formatter = null)
        {
            this.Mapping = mapping;
            this.Formatter = formatter;
        }
    }
}

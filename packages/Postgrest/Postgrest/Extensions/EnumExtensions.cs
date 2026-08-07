using System;
namespace Supabase.Postgrest.Extensions
{

	/// <summary>
	/// Adds functionality to get a typed Attribute attached to an enum value.
	/// </summary>
	public static class EnumExtensions
	{
		/// <summary>
		/// Gets a typed Attribute attached to an enum value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		internal static T? GetAttribute<T>(this Enum value) where T : Attribute
		{
			var type = value.GetType();
			var name = Enum.GetName(type, value);

			if (name == null)
			{
				return null;
			}

			var fieldInfo = type.GetField(name);

			if (fieldInfo == null)
			{
				return null;
			}

			if (Attribute.GetCustomAttribute(fieldInfo, typeof(T)) is T attribute)
			{
				return attribute;
			}

			return null;
		}
	}
}

using System;
using System.Runtime.CompilerServices;
#pragma warning disable CS1591

namespace Supabase.Postgrest.Attributes
{

	/// <summary>
	/// Used to map a C# property to a Postgrest PrimaryKey.
	/// </summary>
	/// <example>
	/// <code>
	/// class User : BaseModel {
	///     [PrimaryKey("id")]
	///     public string Id {get; set;}
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute : Attribute
	{
		public string ColumnName { get; }

		/// <summary>
		/// Would be set to false in the event that the database handles the generation of this property.
		/// </summary>
		public bool ShouldInsert { get; }

		public PrimaryKeyAttribute([CallerMemberName] string? columnName = null, bool shouldInsert = false)
		{
			ColumnName = columnName!; // Either given by user or specified by runtime compiler.
			ShouldInsert = shouldInsert;
		}
	}
}

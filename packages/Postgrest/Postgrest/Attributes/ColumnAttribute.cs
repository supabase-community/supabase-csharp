using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
namespace Supabase.Postgrest.Attributes
{

	/// <summary>
	/// Used to map a C# property to a Postgrest Column.
	/// </summary>
	/// <example>
	/// <code>
	/// class User : BaseModel {
	///     [ColumnName("firstName")]
	///     public string FirstName {get; set;}
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Property)]
	public class ColumnAttribute : Attribute
	{
		/// <summary>
		/// The name in postgres of this column.
		/// </summary>
		public string ColumnName { get; }

		/// <summary>
		/// Specifies what should be serialized in the event this column's value is NULL
		/// </summary>
		public NullValueHandling NullValueHandling { get; set; }

		/// <summary>
		/// If the performed query is an Insert or Upsert, should this value be ignored?
		/// </summary>
		public bool IgnoreOnInsert { get; }

		/// <summary>
		/// If the performed query is an Update, should this value be ignored?
		/// </summary>
		public bool IgnoreOnUpdate { get; }

		/// <inheritdoc />
		public ColumnAttribute([CallerMemberName] string? columnName = null, NullValueHandling nullValueHandling = NullValueHandling.Include, bool ignoreOnInsert = false, bool ignoreOnUpdate = false)
		{
			ColumnName = columnName!; // Will either be user specified or given by runtime compiler.
			NullValueHandling = nullValueHandling;
			IgnoreOnInsert = ignoreOnInsert;
			IgnoreOnUpdate = ignoreOnUpdate;
		}
	}
}

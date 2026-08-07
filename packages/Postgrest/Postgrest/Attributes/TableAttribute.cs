using System;
#pragma warning disable CS1591
namespace Supabase.Postgrest.Attributes
{

	/// <summary>
	/// Used to map a C# Model to a Postgres Table.
	/// </summary>
	/// <example>
	/// <code>
	/// [Table("user")]
	/// class User : BaseModel {
	///     [ColumnName("firstName")]
	///     public string FirstName {get; set;}
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Class)]
	public class TableAttribute : Attribute
	{
		public string Name { get; set; }

		public TableAttribute(string tableName)
		{
			Name = tableName;
		}
	}
}

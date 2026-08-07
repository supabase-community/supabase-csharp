namespace Supabase.Postgrest.Extensions
{

	/// <summary>
	/// Adds functionality to transform a C# Range to a Postgrest String.
	/// 
	/// <see>
	///     <cref>https://www.postgresql.org/docs/14/rangetypes.html</cref>
	/// </see>
	/// </summary>
	public static class RangeExtensions
	{
		/// <summary>
		/// Transforms a C# Range to a Postgrest String.
		/// </summary>
		/// <param name="range"></param>
		/// <returns></returns>
		internal static string ToPostgresString(this IntRange range) => $"[{range.Start},{range.End}]";
	}
}

using Supabase.Core.Attributes;
#pragma warning disable CS1591
namespace Supabase.Postgrest
{

	public static class Constants
	{
		/// <summary>
		/// See: https://postgrest.org/en/v7.0.0/api.html?highlight=operators#operators
		/// </summary>
		public enum Operator
		{
			[MapTo("and")]
			And,
			[MapTo("or")]
			Or,
			[MapTo("eq")]
			Equals,
			[MapTo("gt")]
			GreaterThan,
			[MapTo("gte")]
			GreaterThanOrEqual,
			[MapTo("lt")]
			LessThan,
			[MapTo("lte")]
			LessThanOrEqual,
			[MapTo("neq")]
			NotEqual,
			[MapTo("like")]
			Like,
			[MapTo("ilike")]
			ILike,
			[MapTo("in")]
			In,
			[MapTo("is")]
			Is,
			[MapTo("fts")]
			FTS,
			[MapTo("plfts")]
			PLFTS,
			[MapTo("phfts")]
			PHFTS,
			[MapTo("wfts")]
			WFTS,
			[MapTo("cs")]
			Contains,
			[MapTo("cd")]
			ContainedIn,
			[MapTo("ov")]
			Overlap,
			[MapTo("sl")]
			StrictlyLeft,
			[MapTo("sr")]
			StrictlyRight,
			[MapTo("nxr")]
			NotRightOf,
			[MapTo("nxl")]
			NotLeftOf,
			[MapTo("adj")]
			Adjacent,
			[MapTo("not")]
			Not,
		}

		public enum Ordering
		{
			[MapTo("asc")]
			Ascending,
			[MapTo("desc")]
			Descending,
		}

		/// <summary>
		/// See: https://postgrest.org/en/v7.0.0/api.html?highlight=nulls%20first#ordering
		/// </summary>
		public enum NullPosition
		{
			[MapTo("nullsfirst")]
			First,
			[MapTo("nullslast")]
			Last
		}

		/// <summary>
		/// See: https://postgrest.org/en/v7.0.0/api.html?highlight=count#estimated-count
		/// </summary>
		public enum CountType
		{
			[MapTo("exact")]
			Exact,
			[MapTo("planned")]
			Planned,
			[MapTo("estimated")]
			Estimated
		}
	}
}

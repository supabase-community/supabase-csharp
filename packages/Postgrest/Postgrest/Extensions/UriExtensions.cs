using System;
namespace Supabase.Postgrest.Extensions
{
	/// <summary>
	/// Pull the instance info out of the Uri
	/// </summary>
	public static class UriExtensions
	{
		/// <summary>
		/// Pull the instance info out of the Uri
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static string GetInstanceUrl(this Uri uri) =>
			uri.GetLeftPart(UriPartial.Authority) + uri.LocalPath;
	}
}

namespace Supabase.Gotrue.Mfa
{
	public class AmrEntry
	{
		/// <summary>
		/// Authentication method name.
		/// </summary>
		public string Method { get; set; }

		/// <summary>
		/// Timestamp when the method was successfully used. Represents number of
		/// seconds since 1st January 1970 (UNIX epoch) in UTC.
		/// </summary>
		public long Timestamp { get; set; }
	}
}
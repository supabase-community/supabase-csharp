using System;
using System.Threading;

namespace Supabase.Storage
{
	/// <summary>
	/// Options that can be passed into the Storage Client
	/// </summary>
	public class ClientOptions
	{
		/// <summary>
		/// The timespan to wait before an HTTP Upload Timesout
		/// See: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=net-7.0
		/// </summary>
		public TimeSpan HttpUploadTimeout = Timeout.InfiniteTimeSpan;

		/// <summary>
		/// The timespan to wait before an HTTP Upload Timesout
		/// See: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=net-7.0
		/// </summary>
		public TimeSpan HttpDownloadTimeout = Timeout.InfiniteTimeSpan;

		/// <summary>
		/// The timespan to wait before an HTTP Client request times out.
		/// See: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=net-7.0
		/// </summary>
		public TimeSpan HttpRequestTimeout = TimeSpan.FromSeconds(100);
	}
}

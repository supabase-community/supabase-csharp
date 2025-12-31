using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Supabase.Storage.Exceptions;
using System.Threading;

[assembly: InternalsVisibleTo("StorageTests")]
namespace Supabase.Storage
{
	internal static class Helpers
	{
		internal static HttpClient? HttpRequestClient;

		internal static HttpClient? HttpUploadClient;

		internal static HttpClient? HttpDownloadClient;

		/// <summary>
		/// Initializes HttpClients with their appropriate timeouts. Called at the initialization of StorageBucketApi.
		/// </summary>
		/// <param name="options"></param>
		internal static void Initialize(ClientOptions options)
		{
			HttpRequestClient = new HttpClient { Timeout = options.HttpRequestTimeout };
			HttpDownloadClient = new HttpClient { Timeout = options.HttpDownloadTimeout };
			HttpUploadClient = new HttpClient { Timeout = options.HttpUploadTimeout };
		}

		/// <summary>
		/// Helper to make a request using the defined parameters to an API Endpoint and coerce into a model. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="method"></param>
		/// <param name="url"></param>
		/// <param name="data"></param>
		/// <param name="headers"></param>
		/// <returns></returns>
		public static async Task<T?> MakeRequest<T>(HttpMethod method, string url, object? data = null,
			Dictionary<string, string>? headers = null) where T : class
		{
			var response = await MakeRequest(method, url, data, headers);
			var content = await response.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<T>(content);
		}

		/// <summary>
		/// Helper to make a request using the defined parameters to an API Endpoint.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="url"></param>
		/// <param name="data"></param>
		/// <param name="headers"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<HttpResponseMessage> MakeRequest(HttpMethod method, string url, object? data = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
		{
			var builder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(builder.Query);

			if (data != null && method != HttpMethod.Get)
			{
				// Case if it's a Get request the data object is a dictionary<string,string>
				if (data is Dictionary<string, string> reqParams)
				{
					foreach (var param in reqParams)
						query[param.Key] = param.Value;
				}
			}

			builder.Query = query.ToString();

			using var requestMessage = new HttpRequestMessage(method, builder.Uri);
			
			if (data != null && method != HttpMethod.Get)
				requestMessage.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

			if (headers != null)
			{
				foreach (var kvp in headers)
					requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
			}

			var response = await HttpRequestClient!.SendAsync(requestMessage, cancellationToken);

			var content = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(content);
				var e = new SupabaseStorageException(errorResponse?.Message ?? content)
				{
					Content = content,
					Response = response,
					StatusCode = errorResponse?.StatusCode ?? (int)response.StatusCode
				};
					
				e.AddReason();
				throw e;
			}
				
			return response;
		}
	}

	public class GenericResponse
	{
		[JsonProperty("message")]
		public string? Message { get; set; }
	}

	public class ErrorResponse
	{
		[JsonProperty("statusCode")]
		public int StatusCode { get; set; }
		
		[JsonProperty("message")]
		public string? Message { get; set; }
	}
}
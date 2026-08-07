using System.Net.Http;
using Newtonsoft.Json;
#pragma warning disable CS1591
namespace Supabase.Postgrest.Responses
{

	/// <summary>
	/// A wrapper class from which all Responses derive.
	/// </summary>
	public class BaseResponse
	{
		[JsonIgnore]
		public HttpResponseMessage? ResponseMessage { get; set; }

		[JsonIgnore]
		public string? Content { get; set; }

		[JsonIgnore]
		public ClientOptions ClientOptions { get; set; }

		public BaseResponse(ClientOptions clientOptions, HttpResponseMessage? responseMessage, string? content)
		{
			ClientOptions = clientOptions;
			ResponseMessage = responseMessage;
			Content = content;
		}
	}
}

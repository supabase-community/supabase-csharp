using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase.Postgrest.Extensions;
using Supabase.Postgrest.Models;

namespace Supabase.Postgrest.Responses
{

	/// <summary>
	/// A representation of a successful Postgrest response that transforms the string response into a C# Modelled response.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ModeledResponse<T> : BaseResponse where T : BaseModel, new()
	{
		/// <summary>
		/// The first model in the response.
		/// </summary>
		public T? Model => Models.FirstOrDefault();

		/// <summary>
		/// A list of models in the response.
		/// </summary>
		public List<T> Models { get; } = new();

		/// <summary>
		/// The number of results matching the specified filters
		/// </summary>
		public int Count = 0;
		
		/// <inheritdoc />
		public ModeledResponse(BaseResponse baseResponse, JsonSerializerSettings serializerSettings, Func<Dictionary<string, string>>? getHeaders = null, bool shouldParse = true) : base(baseResponse.ClientOptions, baseResponse.ResponseMessage, baseResponse.Content)
		{
			Content = baseResponse.Content;
			ResponseMessage = baseResponse.ResponseMessage;

			if (!shouldParse || string.IsNullOrEmpty(Content)) return;

			var token = JToken.Parse(Content!);

			switch (token)
			{
				// A List of models has been returned
				case JArray: {
					var deserialized = JsonConvert.DeserializeObject<List<T>>(Content!, serializerSettings);

					if (deserialized != null)
						Models = deserialized;

					foreach (var model in Models)
					{
						model.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetInstanceUrl().Replace(model.TableName, "").TrimEnd('/');
						model.RequestClientOptions = ClientOptions;
						model.GetHeaders = getHeaders;
					}

					break;
				}
				// A single model has been returned
				case JObject: {
					Models.Clear();

					var obj = JsonConvert.DeserializeObject<T>(Content!, serializerSettings);

					if (obj != null)
					{
						obj.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetInstanceUrl().Replace(obj.TableName, "").TrimEnd('/');
						obj.RequestClientOptions = ClientOptions;
						obj.GetHeaders = getHeaders;

						Models.Add(obj);
					}

					break;
				}
			}

			try
			{
				var countStr = baseResponse.ResponseMessage?.Content.Headers.GetValues("Content-Range")
					.FirstOrDefault();
				Count = int.Parse(countStr?.Split('/')[1] ?? throw new InvalidOperationException());
			}
			catch (Exception e)
			{
				Debugger.Instance.Log(this, e.Message);
				Count = -1;
			}

			Debugger.Instance.Log(this, $"Response: [{baseResponse.ResponseMessage?.StatusCode}]\n" + $"Parsed Models <{typeof(T).Name}>:\n\t{JsonConvert.SerializeObject(Models)}\n");
		}
	}
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Supabase.Core;
using Supabase.Core.Extensions;
using Supabase.Functions.Exceptions;
using Supabase.Functions.Interfaces;

[assembly: InternalsVisibleTo("FunctionsTests")]

namespace Supabase.Functions
{
    /// <inheritdoc />
    public partial class Client : IFunctionsClient
    {
        private HttpClient _httpClient = new HttpClient();
        private readonly string _baseUrl;
        private readonly FunctionRegion _region;

        /// <summary>
        /// Function that can be set to return dynamic headers.
        ///
        /// Headers specified in the method parameters will ALWAYS take precedence over headers returned by this function.
        /// </summary>
        public Func<Dictionary<string, string>>? GetHeaders { get; set; }

        /// <summary>
        /// Initializes a functions client
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="region"></param>
        public Client(string baseUrl, FunctionRegion? region = null)
        {
            _baseUrl = baseUrl;
            _region = region ?? FunctionRegion.Any;
        }

        /// <summary>
        /// Returns an <see cref="HttpContent"/> response, allowing for coersion into Streams, Strings, and byte[]
        /// </summary>
        /// <param name="functionName">Function Name, will be appended to BaseUrl</param>
        /// <param name="token">Anon Key.</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public async Task<HttpContent> RawInvoke(
            string functionName,
            string? token = null,
            InvokeFunctionOptions? options = null
        )
        {
            var url = $"{_baseUrl}/{functionName}";

            return (await HandleRequest(url, token, options)).Content;
        }

        /// <summary>
        /// Invokes a function and returns the Text content of the response.
        /// </summary>
        /// <param name="functionName">Function Name, will be appended to BaseUrl</param>
        /// <param name="token">Anon Key.</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public async Task<string> Invoke(
            string functionName,
            string? token = null,
            InvokeFunctionOptions? options = null
        )
        {
            var url = $"{_baseUrl}/{functionName}";
            var response = await HandleRequest(url, token, options);

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Invokes a function and returns a JSON Deserialized object according to the supplied generic Type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functionName">Function Name, will be appended to BaseUrl</param>
        /// <param name="token">Anon Key.</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public async Task<T?> Invoke<T>(
            string functionName,
            string? token = null,
            InvokeFunctionOptions? options = null
        )
            where T : class
        {
            var url = $"{_baseUrl}/{functionName}";
            var response = await HandleRequest(url, token, options);

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(content);
        }

        /// <summary>
        /// Internal request handling
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="FunctionsException"></exception>
        private async Task<HttpResponseMessage> HandleRequest(
            string url,
            string? token = null,
            InvokeFunctionOptions? options = null
        )
        {
            options ??= new InvokeFunctionOptions();

            if (GetHeaders != null)
            {
                options.Headers = GetHeaders().MergeLeft(options.Headers);
            }

            if (!string.IsNullOrEmpty(token))
            {
                options.Headers["Authorization"] = $"Bearer {token}";
            }

            options.Headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

            var region = options.FunctionRegion;
            if (region == null)
            {
                region = _region;
            }

            if (region != FunctionRegion.Any)
            {
                options.Headers["x-region"] = region.ToString();
            }

            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            builder.Query = query.ToString();

            using var requestMessage = new HttpRequestMessage(options.HttpMethod, builder.Uri);
            requestMessage.Content = new StringContent(
                JsonConvert.SerializeObject(options.Body),
                Encoding.UTF8,
                "application/json"
            );

            foreach (var kvp in options.Headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }

            if (_httpClient.Timeout != options.HttpTimeout)
            {
                _httpClient = new HttpClient();
                _httpClient.Timeout = options.HttpTimeout;
            }

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode && !response.Headers.Contains("x-relay-error"))
                return response;

            var content = await response.Content.ReadAsStringAsync();
            var exception = new FunctionsException(content)
            {
                Content = content,
                Response = response,
                StatusCode = (int)response.StatusCode,
            };
            exception.AddReason();
            throw exception;
        }
    }
}

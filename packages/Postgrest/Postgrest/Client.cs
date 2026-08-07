using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Supabase.Core.Extensions;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;

namespace Supabase.Postgrest
{
    /// <inheritdoc />
    public class Client : IPostgrestClient
    {
        /// <summary>
        /// Custom Serializer resolvers and converters that will be used for encoding and decoding Postgrest JSON responses.
        ///
        /// By default, Postgrest seems to use a date format that C# and Newtonsoft do not like, so this initial
        /// configuration handles that.
        /// </summary>
        public static JsonSerializerSettings SerializerSettings(ClientOptions? options = null)
        {
            options ??= new ClientOptions();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new PostgrestContractResolver(),
                Converters =
                {
                    new IsoDateTimeConverter
                    {
                        DateTimeStyles = options.DateTimeStyles,
                        DateTimeFormat = ClientOptions.DATE_TIME_FORMAT
                    }
                }
            };

           
            if (options.SerializeEnumsAsStrings)
                settings.Converters.Add(new StringEnumConverter());

            return settings;
        }

        /// <inheritdoc />
        public string BaseUrl { get; }

        /// <inheritdoc />
        public ClientOptions Options { get; }

        /// <inheritdoc />
        public void AddRequestPreparedHandler(OnRequestPreparedEventHandler handler) =>
            Hooks.Instance.AddRequestPreparedHandler(handler);

        /// <inheritdoc />
        public void RemoveRequestPreparedHandler(OnRequestPreparedEventHandler handler) =>
            Hooks.Instance.AddRequestPreparedHandler(handler);

        /// <inheritdoc />
        public void ClearRequestPreparedHandlers() =>
            Hooks.Instance.ClearRequestPreparedHandlers();

        /// <inheritdoc />
        [Obsolete("The debug handler is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Postgrest\". This member will be removed in a future major version.")]
        public void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler) =>
            Debugger.Instance.AddDebugHandler(handler);

        /// <inheritdoc />
        [Obsolete("The debug handler is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Postgrest\". This member will be removed in a future major version.")]
        public void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler) =>
            Debugger.Instance.RemoveDebugHandler(handler);

        /// <inheritdoc />
        [Obsolete("The debug handler is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Postgrest\". This member will be removed in a future major version.")]
        public void ClearDebugHandlers() => Debugger.Instance.ClearDebugHandlers();

        /// <summary>
        /// Function that can be set to return dynamic headers.
        /// 
        /// Headers specified in the constructor options will ALWAYS take precedence over headers returned by this function.
        /// </summary>
        public Func<Dictionary<string, string>>? GetHeaders { get; set; }

        /// <summary>
        /// Should be the first call to this class to initialize a connection with a Postgrest API Server
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="options">Optional client configuration.</param>
        /// <returns></returns>
        public Client(string baseUrl, ClientOptions? options = null)
        {
            BaseUrl = baseUrl;
            Options = options ?? new ClientOptions();
        }


        /// <inheritdoc />
        public IPostgrestTable<T> Table<T>() where T : BaseModel, new() =>
            new Table<T>(BaseUrl, SerializerSettings(Options), Options)
            {
                GetHeaders = GetHeaders
            };

        /// <inheritdoc />
        public IPostgrestTableWithCache<T> Table<T>(IPostgrestCacheProvider cacheProvider)
            where T : BaseModel, new() =>
            new TableWithCache<T>(BaseUrl, cacheProvider, SerializerSettings(Options), Options)
            {
                GetHeaders = GetHeaders
            };

        /// <inheritdoc />
        public T Attach<T>(T model) where T : BaseModel
        {
            model.BaseUrl = BaseUrl;
            model.RequestClientOptions = Options;
            model.GetHeaders = GetHeaders;
            return model;
        }


        /// <inheritdoc />
        public async Task<TModeledResponse?> Rpc<TModeledResponse>(string procedureName, object? parameters = null)
        {
            var response = await Rpc(procedureName, parameters);

            return string.IsNullOrEmpty(response.Content) ? default : JsonConvert.DeserializeObject<TModeledResponse>(response.Content!);
        }

        /// <inheritdoc />
        public Task<BaseResponse> Rpc(string procedureName, object? parameters = null)
        {
            // Build Uri
            var builder = new UriBuilder($"{BaseUrl}/rpc/{procedureName}");

            var canonicalUri = builder.Uri.ToString();

            var serializerSettings = SerializerSettings(Options);

            // Prepare parameters
            Dictionary<string, object>? data = null;
            if (parameters != null)
                data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(parameters, serializerSettings));

            // Prepare headers
            var headers = Helpers.PrepareRequestHeaders(HttpMethod.Post,
                new Dictionary<string, string>(Options.Headers), Options);

            if (GetHeaders != null)
                headers = GetHeaders().MergeLeft(headers);

            // Send request
            var request =
                Helpers.MakeRequest(Options, HttpMethod.Post, canonicalUri, serializerSettings, data, headers, operation: "rpc");
            return request;
        }
    }
}
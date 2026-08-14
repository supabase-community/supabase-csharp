using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Supabase.Core;
using Supabase.Core.Diagnostics;
using Supabase.Core.Extensions;
using Supabase.Functions.Exceptions;
using Supabase.Functions.Interfaces;

[assembly: InternalsVisibleTo("Functions.Tests")]

namespace Supabase.Functions;

/// <inheritdoc />
public partial class Client : IFunctionsClient
{
    /// <summary>
    /// Serialization settings tuned to match the previous Newtonsoft.Json behavior: case-insensitive
    /// property matching on deserialize, and relaxed escaping so characters such as '+', '&amp;' and
    /// non-ASCII are written literally rather than as \u escapes.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private HttpClient httpClient = new HttpClient();
    private readonly string baseUrl;
    private readonly FunctionRegion region;

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
        this.baseUrl = baseUrl;
        this.region = region ?? FunctionRegion.Any;
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
        var url = $"{this.baseUrl}/{functionName}";

        return (await this.HandleRequest(functionName, url, token, options)).Content;
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
        var url = $"{this.baseUrl}/{functionName}";
        var response = await this.HandleRequest(functionName, url, token, options);

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
        var url = $"{this.baseUrl}/{functionName}";
        var response = await this.HandleRequest(functionName, url, token, options);

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<T>(content, SerializerOptions);
    }

    /// <summary>
    /// Internal request handling
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="url"></param>
    /// <param name="token"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="FunctionsException"></exception>
    private async Task<HttpResponseMessage> HandleRequest(
        string functionName,
        string url,
        string? token = null,
        InvokeFunctionOptions? options = null
    )
    {
        options ??= new InvokeFunctionOptions();

        if (this.GetHeaders != null)
        {
            options.Headers = this.GetHeaders().MergeLeft(options.Headers);
        }

        if (!string.IsNullOrEmpty(token))
        {
            options.Headers["Authorization"] = $"Bearer {token}";
        }

        options.Headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

        var region = options.FunctionRegion;
        if (region == null)
        {
            region = this.region;
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
            JsonSerializer.Serialize(options.Body, SerializerOptions),
            Encoding.UTF8,
            "application/json"
        );

        foreach (var kvp in options.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }

        if (this.httpClient.Timeout != options.HttpTimeout)
        {
            this.httpClient = new HttpClient();
            this.httpClient.Timeout = options.HttpTimeout;
        }

        using var activity = FunctionsInstrumentation.StartInvokeActivity(options.HttpMethod, builder.Uri, functionName);
        var startTimestamp = Stopwatch.GetTimestamp();
        int? statusCode = null;
        string? errorType = null;

        try
        {
            var response = await this.httpClient.SendAsync(requestMessage);
            statusCode = (int) response.StatusCode;
            var isRelayError = response.Headers.Contains("x-relay-error");
            activity.SetHttpResponseTags(statusCode.Value);

            if (response.IsSuccessStatusCode && !isRelayError)
                return response;

            // A relay error is a failure even when the status code is a success, so it is not
            // covered by SetHttpResponseTags' status-code check above.
            if (isRelayError)
            {
                errorType = "x-relay-error";
                activity?.SetTag("error.type", errorType);
                activity?.SetStatus(ActivityStatusCode.Error);
            }
            else
            {
                errorType = statusCode.Value.ToString();
            }

            var content = await response.Content.ReadAsStringAsync();
            var exception = new FunctionsException(content)
            {
                Content = content,
                Response = response,
                StatusCode = (int) response.StatusCode,
            };
            exception.AddReason();
            throw exception;
        }
        catch (Exception e) when (!(e is FunctionsException))
        {
            // Transport-level failures (no response); Functions surfaces these raw, so tag and rethrow.
            errorType = e.GetType().FullName;
            activity.SetFailure(e);
            throw;
        }
        finally
        {
            FunctionsInstrumentation.RecordInvoke(options.HttpMethod, builder.Uri, functionName, statusCode, errorType, startTimestamp);
        }
    }
}

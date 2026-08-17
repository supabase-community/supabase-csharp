using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace Supabase.Postgrest;

/// <summary>
/// Delegate representing the request to be sent to the remote server.
/// </summary>
public delegate void OnRequestPreparedEventHandler(object sender, ClientOptions clientOptions,
    HttpMethod method, string url,
    JsonSerializerOptions serializerSettings, object? data = null,
    Dictionary<string, string>? headers = null);

/// <summary>
/// A internal singleton used for hooks applied to <see cref="Client"/> and <see cref="Table{T}"/>
/// </summary>
internal class Hooks
{
    private static Hooks? instance { get; set; }

    /// <summary>
    /// Returns the Singleton Instance.
    /// </summary>
    public static Hooks Instance
    {
        get
        {
            instance ??= new Hooks();
            return instance;
        }
    }

    private readonly List<OnRequestPreparedEventHandler> requestPreparedEventHandlers =
        new List<OnRequestPreparedEventHandler>();

    private Hooks()
    {
    }

    /// <summary>
    /// Adds a handler that is called prior to a request being sent.
    /// </summary>
    /// <param name="handler"></param>
    public void AddRequestPreparedHandler(OnRequestPreparedEventHandler handler)
    {
        if (!this.requestPreparedEventHandlers.Contains(handler))
            this.requestPreparedEventHandlers.Add(handler);
    }

    /// <summary>
    /// Removes an <see cref="OnRequestPreparedEventHandler"/> handler.
    /// </summary>
    /// <param name="handler"></param>
    public void RemoveRequestPreparedHandler(OnRequestPreparedEventHandler handler)
    {
        if (this.requestPreparedEventHandlers.Contains(handler))
            this.requestPreparedEventHandlers.Remove(handler);
    }

    /// <summary>
    /// Clears all <see cref="OnRequestPreparedEventHandler"/> handlers.
    /// </summary>
    public void ClearRequestPreparedHandlers() => this.requestPreparedEventHandlers.Clear();

    /// <summary>
    /// Notifies all listeners.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="clientOptions"></param>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="serializerSettings"></param>
    /// <param name="data"></param>
    /// <param name="headers"></param>
    public void NotifyOnRequestPreparedHandlers(object sender, ClientOptions clientOptions, HttpMethod method,
        string url,
        JsonSerializerOptions serializerSettings, object? data = null,
        Dictionary<string, string>? headers = null)
    {
        Debugger.Instance.Log(this, $"{nameof(NotifyOnRequestPreparedHandlers)} called for [{method}] to {url}");

        foreach (var handler in this.requestPreparedEventHandlers.ToList())
            handler.Invoke(sender, clientOptions, method, url, serializerSettings, data, headers);
    }
}

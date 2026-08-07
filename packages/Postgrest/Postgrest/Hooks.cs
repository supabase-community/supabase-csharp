using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace Supabase.Postgrest
{
    /// <summary>
    /// Delegate representing the request to be sent to the remote server.
    /// </summary>
    public delegate void OnRequestPreparedEventHandler(object sender, ClientOptions clientOptions,
        HttpMethod method, string url,
        JsonSerializerSettings serializerSettings, object? data = null,
        Dictionary<string, string>? headers = null);

    /// <summary>
    /// A internal singleton used for hooks applied to <see cref="Client"/> and <see cref="Table{T}"/>
    /// </summary>
    internal class Hooks
    {
        private static Hooks? _instance { get; set; }

        /// <summary>
        /// Returns the Singleton Instance.
        /// </summary>
        public static Hooks Instance
        {
            get
            {
                _instance ??= new Hooks();
                return _instance;
            }
        }

        private readonly List<OnRequestPreparedEventHandler> _requestPreparedEventHandlers =
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
            if (!_requestPreparedEventHandlers.Contains(handler))
                _requestPreparedEventHandlers.Add(handler);
        }

        /// <summary>
        /// Removes an <see cref="OnRequestPreparedEventHandler"/> handler.
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveRequestPreparedHandler(OnRequestPreparedEventHandler handler)
        {
            if (_requestPreparedEventHandlers.Contains(handler))
                _requestPreparedEventHandlers.Remove(handler);
        }

        /// <summary>
        /// Clears all <see cref="OnRequestPreparedEventHandler"/> handlers.
        /// </summary>
        public void ClearRequestPreparedHandlers()
        {
            _requestPreparedEventHandlers.Clear();
        }

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
            JsonSerializerSettings serializerSettings, object? data = null,
            Dictionary<string, string>? headers = null)
        {
            Debugger.Instance.Log(this, $"{nameof(NotifyOnRequestPreparedHandlers)} called for [{method}] to {url}");

            foreach (var handler in _requestPreparedEventHandlers.ToList())
                handler.Invoke(sender, clientOptions, method, url, serializerSettings, data, headers);
        }
    }
}
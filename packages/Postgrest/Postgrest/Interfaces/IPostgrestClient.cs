using System;
using System.Threading.Tasks;
using Supabase.Core.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;

namespace Supabase.Postgrest.Interfaces
{
    /// <summary>
    /// Client interface for Postgrest
    /// </summary>
    public interface IPostgrestClient : IGettableHeaders
    {
        /// <summary>
        /// API Base Url for subsequent calls.
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// The Options <see cref="Client"/> was initialized with.
        /// </summary>
        ClientOptions Options { get; }

        /// <summary>
        /// Adds a handler that is called prior to a request being sent.
        /// </summary>
        /// <param name="handler"></param>
        void AddRequestPreparedHandler(OnRequestPreparedEventHandler handler);

        /// <summary>
        /// Removes an <see cref="OnRequestPreparedEventHandler"/> handler.
        /// </summary>
        /// <param name="handler"></param>
        void RemoveRequestPreparedHandler(OnRequestPreparedEventHandler handler);

        /// <summary>
        /// Clears all <see cref="OnRequestPreparedEventHandler"/> handlers.
        /// </summary>
        void ClearRequestPreparedHandlers();

        /// <summary>
        /// Adds a debug handler
        /// </summary>
        /// <param name="handler"></param>
        [Obsolete("The debug handler is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Postgrest\". This member will be removed in a future major version.")]
        void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler);

        /// <summary>
        /// Removes a debug handler
        /// </summary>
        /// /// <param name="handler"></param>
        [Obsolete("The debug handler is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Postgrest\". This member will be removed in a future major version.")]
        void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler);

        /// <summary>
        /// Clears debug handlers
        /// </summary>
        [Obsolete("The debug handler is replaced by OpenTelemetry-compatible diagnostics: subscribe to the ActivitySource and Meter named \"Supabase.Postgrest\". This member will be removed in a future major version.")]
        void ClearDebugHandlers();

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <returns></returns>
        Task<BaseResponse> Rpc(string procedureName, object? parameters);

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <typeparam name="TModeledResponse">A type used for hydrating the HTTP response content (hydration through JSON.NET)</typeparam>
        /// <returns>A hydrated model</returns>
        Task<TModeledResponse?> Rpc<TModeledResponse>(string procedureName, object? parameters = null);

        /// <summary>
        /// Returns a Table Query Builder instance for a defined model - representative of `USE $TABLE`
        /// </summary>
        /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
        /// <returns></returns>
        IPostgrestTable<T> Table<T>() where T : BaseModel, new();
        
        /// <summary>
        /// Returns a Table Query Builder instance with a Cache Provider for a defined model - representative of `USE #$TABLE`
        /// </summary>
        /// <param name="cacheProvider"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IPostgrestTableWithCache<T> Table<T>(IPostgrestCacheProvider cacheProvider) where T : BaseModel, new();

        /// <summary>
        /// Attaches this client's context (<see cref="BaseModel.BaseUrl"/>, <see cref="BaseModel.RequestClientOptions"/>,
        /// and its headers callback) to a model, so that <c>Update</c>/<c>Delete</c> can be called directly on the
        /// model afterward.
        ///
        /// Intended for models that were deserialized by something other than this client's own <c>Table&lt;T&gt;</c>
        /// responses (which already attach this context automatically) - for example, a model deserialized from a
        /// Realtime event.
        /// </summary>
        /// <param name="model">The model to attach context to. Mutated in place and also returned for chaining.</param>
        /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
        /// <returns>The same model instance, for convenience.</returns>
        T Attach<T>(T model) where T : BaseModel;
    }
}
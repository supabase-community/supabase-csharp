using System;
using Supabase.Realtime.Exceptions;

namespace Supabase.Realtime.Interfaces;

/// <summary>
/// Contract representing an internal debugger.
/// </summary>
public interface IRealtimeDebugger
{
    /// <summary>
    /// A debug event handler
    /// </summary>
    delegate void DebugEventHandler(object sender, string message, Exception? exception);

    /// <summary>
    /// Adds a debug listener
    /// </summary>
    /// <param name="handler"></param>
    void AddDebugHandler(DebugEventHandler handler);

    /// <summary>
    /// Removes a debug handler.
    /// </summary>
    /// <param name="handler"></param>
    void RemoveDebugHandler(IRealtimeDebugger.DebugEventHandler handler);

    /// <summary>
    /// Clears debug handlers.
    /// </summary>
    void ClearDebugHandlers();

    /// <summary>
    /// Notifies debug listeners
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    void Log(object sender, string message, Exception? exception = null);
}
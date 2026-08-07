using Supabase.Realtime.Socket;
using System;
using System.Threading.Tasks;
using Supabase.Realtime.Models;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime.Interfaces;

/// <summary>
/// A contract representing a realtime broadcast
/// </summary>
public interface IRealtimeBroadcast
{
    /// <summary>
    /// A delegate for broadcast events  
    /// </summary>
    delegate void BroadcastEventHandler(IRealtimeBroadcast sender, BaseBroadcast? broadcast);

    /// <summary>
    /// Adds a broadcast event handler
    /// </summary>
    /// <param name="broadcastEventHandler"></param>
    void AddBroadcastEventHandler(BroadcastEventHandler broadcastEventHandler);
        
    /// <summary>
    /// Removes a broadcast event handler
    /// </summary>
    /// <param name="broadcastEventHandler"></param>
    void RemoveBroadcastEventHandler(BroadcastEventHandler broadcastEventHandler);
        
    /// <summary>
    /// Clears all broadcast event handlers
    /// </summary>
    void ClearBroadcastEventHandlers();

    /// <summary>
    /// Sends a broadcast to a given event name with an arbitrary, serializable payload.
    /// </summary>
    /// <param name="broadcastEventName"></param>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    /// <returns></returns>
    Task<bool> Send(string? broadcastEventName, object payload, int timeoutMs = DefaultTimeout);

    /// <summary>
    /// An internal trigger used for notifying event delegates.
    /// </summary>
    /// <param name="response"></param>
    internal void TriggerReceived(SocketResponse response);
}
using Supabase.Realtime.Socket;
using System;
using System.Threading.Tasks;
using Supabase.Realtime.Channel;
using Supabase.Realtime.Models;
using static Supabase.Realtime.Constants;

namespace Supabase.Realtime.Interfaces;

/// <summary>
/// Contract representing a Realtime Presence class
/// </summary>
public interface IRealtimePresence
{
    /// <summary>
    /// Delegate for a presence event.
    /// </summary>
    delegate void PresenceEventHandler(IRealtimePresence sender, EventType eventType);

    /// <summary>
    /// Mapping of presence event types
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Sync event (both join and leave)
        /// </summary>
        Sync,
        /// <summary>
        /// Join event
        /// </summary>
        Join,
        /// <summary>
        /// Leave event
        /// </summary>
        Leave
    }

    /// <summary>
    /// Send an arbitrary payload as a presence event, MUST be called once to register this client as an active presence.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    Task<Push> Track(object? payload, int timeoutMs = DefaultTimeout);

    /// <summary>
    /// Untracks a client
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="timeoutMs"></param>
    Task<Push> Untrack();

    /// <summary>
    /// Add a presence event handler
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    void AddPresenceEventHandler(EventType eventType, PresenceEventHandler handler);

    /// <summary>
    /// Remove a presence event handler
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    void RemovePresenceEventHandlers(EventType eventType, PresenceEventHandler handler);

    /// <summary>
    /// Clear presence events.
    /// </summary>
    /// <param name="eventType"></param>
    void ClearPresenceEventHandlers(EventType? eventType = null);
        
    internal void TriggerSync(SocketResponse response);
    internal void TriggerDiff(SocketResponse args);
}
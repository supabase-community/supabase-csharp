using Supabase.Realtime.Socket;
using System;

namespace Supabase.Realtime.Interfaces;

/// <summary>
/// Contract representing a "Push" or an outgoing message to the socket server
/// </summary>
/// <typeparam name="TChannel"></typeparam>
/// <typeparam name="TSocketResponse"></typeparam>
public interface IRealtimePush<TChannel, TSocketResponse>
	where TChannel : IRealtimeChannel
	where TSocketResponse : IRealtimeSocketResponse
{
	/// <summary>
	/// Delegate for a message event.
	/// </summary>
	delegate void MessageEventHandler(IRealtimePush<TChannel, TSocketResponse> sender, TSocketResponse message);
		
	/// <summary>
	/// Add a message received handler 
	/// </summary>
	/// <param name="handler"></param>
	void AddMessageReceivedHandler(MessageEventHandler handler);
		
	/// <summary>
	/// Remove a message received handler
	/// </summary>
	/// <param name="handler"></param>
	void RemoveMessageReceivedHandler(MessageEventHandler handler);
		
	/// <summary>
	/// Clear Message received handlers.
	/// </summary>
	void ClearMessageReceivedHandler();
		
	/// <summary>
	/// The calling or parent channel
	/// </summary>
	TChannel Channel { get; }
		
	/// <summary>
	/// The event name this push is registered under.
	/// </summary>
	string EventName { get; }
		
	/// <summary>
	/// Is push sent?
	/// </summary>
	bool IsSent { get; }
		
	/// <summary>
	/// The wrapped SocketRequest
	/// </summary>
	SocketRequest? Message { get; }
		
	/// <summary>
	/// The payload (present in <see cref="Message"/>)
	/// </summary>
	object? Payload { get; }
		
	/// <summary>
	/// A unique ID representing this push.
	/// </summary>
	string? Ref { get; }
		
	/// <summary>
	/// The server's response
	/// </summary>
	IRealtimeSocketResponse? Response { get; }
		
	/// <summary>
	/// A timeout event handler.
	/// </summary>
	event EventHandler? OnTimeout;
		
	/// <summary>
	/// Resend this push, only called on a failed push attempt.
	/// </summary>
	/// <param name="timeoutMs"></param>
	void Resend(int timeoutMs = 10000);
		
	/// <summary>
	/// Send this push.
	/// </summary>
	void Send();
}
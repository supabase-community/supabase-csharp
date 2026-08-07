using Supabase.Postgrest.Exceptions;

namespace Supabase.Postgrest.Interfaces
{
	/// <summary>
	/// Interface for getting debug info from Postgrest
	/// </summary>
	public interface IPostgrestDebugger
	{
		/// <inheritdoc />
		delegate void DebugEventHandler(object? sender, string message, PostgrestException? exception);

		/// <summary>
		/// Adds a debug handler
		/// </summary>
		/// <param name="handler"></param>
		void AddDebugHandler(DebugEventHandler handler);
		/// <summary>
		/// Removes a debug handler
		/// </summary>
		/// <param name="handler"></param>
		void RemoveDebugHandler(DebugEventHandler handler);
		/// <summary>
		/// Clears debug handlers
		/// </summary>
		void ClearDebugHandlers();
		/// <summary>
		/// Logs a message
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="message"></param>
		/// <param name="exception"></param>
		void Log(object? sender, string message, PostgrestException? exception = null);
	}
}

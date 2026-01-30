using System;
using System.Collections.Generic;
namespace Supabase.Gotrue
{
	/// <summary>
	/// Manages the debug listeners for the Gotrue Client. You'll want to install a debug listener
	/// to get debug information back - especially for errors from the background RefreshToken thread.
	/// </summary>
	public class DebugNotification
	{
		private readonly List<Action<string, Exception?>> _debugListeners = new List<Action<string, Exception?>>();

		/// <summary>
		/// Add a debug listener to the Gotrue Client. This will be called with debug information
		/// </summary>
		/// <param name="listener"></param>
		public void AddDebugListener(Action<string, Exception?> listener)
		{
			_debugListeners.Add(listener);
		}

		/// <summary>
		/// Send a debug message to all debug listeners
		/// </summary>
		/// <param name="message"></param>
		/// <param name="e"></param>
		public void Log(string message, Exception? e = null)
		{
			foreach (var l in _debugListeners)
			{
				l.Invoke(message, e);
			}
		}
	}
}

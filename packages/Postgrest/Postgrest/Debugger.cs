using System.Collections.Generic;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Interfaces;

namespace Supabase.Postgrest
{

	/// <summary>
	/// A Singleton used for debug notifications
	/// </summary>
	internal class Debugger
	{
		private static Debugger? _instance { get; set; }

		/// <summary>
		/// Returns the Singleton Instance.
		/// </summary>
		public static Debugger Instance
		{
			get
			{
				_instance ??= new Debugger();
				return _instance;
			}
		}

		private Debugger()
		{ }

		private readonly List<IPostgrestDebugger.DebugEventHandler> _debugListeners = new();

		/// <summary>
		/// Adds a debug listener
		/// </summary>
		/// <param name="handler"></param>
		public void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler)
		{
			if (!_debugListeners.Contains(handler))
				_debugListeners.Add(handler);
		}

		/// <summary>
		/// Removes a debug handler.
		/// </summary>
		/// <param name="handler"></param>
		public void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler)
		{
			if (_debugListeners.Contains(handler))
				_debugListeners.Remove(handler);
		}

		/// <summary>
		/// Clears debug handlers.
		/// </summary>
		public void ClearDebugHandlers() =>
			_debugListeners.Clear();

		/// <summary>
		/// Notifies debug listeners.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="message"></param>
		/// <param name="exception"></param>
		public void Log(object? sender, string message, PostgrestException? exception = null)
		{
			foreach (var l in _debugListeners.ToArray())
				l.Invoke(sender, message, exception);
		}
	}
}

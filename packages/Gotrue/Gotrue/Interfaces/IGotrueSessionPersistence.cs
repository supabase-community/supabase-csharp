namespace Supabase.Gotrue.Interfaces
{
	/// <summary>
	/// Interface for session persistence. As a reminder, make sure you handle exceptions and
	/// other error conditions in your implementation.
	/// </summary>
	public interface IGotrueSessionPersistence<TSession>
		where TSession : Session
	{
		/// <summary>
		/// Saves the session to the persistence implementation.
		/// </summary>
		/// <param name="session"></param>
		public void SaveSession(TSession session);

		/// <summary>
		/// Destroys the session in the persistence implementation. Usually this means
		/// deleting the session file or clearing local storage.
		/// </summary>
		public void DestroySession();

		/// <summary>
		/// Loads the session from the persistence implementation. Returns null if there is no session.
		/// </summary>
		/// <returns></returns>
		public TSession? LoadSession();
	}
}

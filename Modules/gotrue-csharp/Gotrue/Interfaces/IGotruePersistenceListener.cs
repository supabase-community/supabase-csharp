namespace Supabase.Gotrue.Interfaces
{
    /// <summary>
    /// Interface for a session persistence auth state handler.
    /// </summary>
    public interface IGotruePersistenceListener<TSession> where TSession : Session
    {
        /// <summary>
        /// The persistence implementation for the client (e.g. file system, local storage, etc).
        /// </summary>
        IGotrueSessionPersistence<TSession> Persistence { get; }

        /// <summary>
        /// Routes auth state changes to the persistence implementation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="stateChanged"></param>
        public void EventHandler(IGotrueClient<User, TSession> sender, Constants.AuthState stateChanged);
    }
}
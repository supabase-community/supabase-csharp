using System;
using Supabase.Gotrue.Interfaces;
namespace Supabase.Gotrue
{
	/// <summary>
	/// Manages the persistence of the Gotrue Session. You'll want to install a persistence listener
	/// to persist user sessions between app restarts. 
	/// </summary>
	public class PersistenceListener : IGotruePersistenceListener<Session>
	{
		/// <summary>
		/// Create a new persistence listener
		/// </summary>
		/// <param name="persistence"></param>
		public PersistenceListener(IGotrueSessionPersistence<Session> persistence)
		{
			Persistence = persistence;
		}

		/// <inheritdoc />
		public IGotrueSessionPersistence<Session> Persistence { get; }

		/// <summary>
		/// If you install a persistence listener, it will be called when the user signs in and signs out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stateChanged"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void EventHandler(IGotrueClient<User, Session> sender, Constants.AuthState stateChanged)
		{
			switch (stateChanged)
			{
				case Constants.AuthState.SignedIn:
				case Constants.AuthState.MfaChallengeVerified:
					if (sender == null)
						throw new ArgumentException("Tried to save a null session (1)");
					if (sender.CurrentSession == null)
						throw new ArgumentException("Tried to save a null session (2)");

					Persistence.SaveSession(sender.CurrentSession);
					break;
				case Constants.AuthState.SignedOut:
					Persistence.DestroySession();
					break;
				case Constants.AuthState.UserUpdated:
					if (sender == null)
						throw new ArgumentException("Tried to save a null session (1)");
					if (sender.CurrentSession == null)
						throw new ArgumentException("Tried to save a null session (2)");

					Persistence.SaveSession(sender.CurrentSession);
					break;
				case Constants.AuthState.PasswordRecovery: break;
				case Constants.AuthState.TokenRefreshed:
					if (sender.CurrentSession != null)
					{
						Persistence.SaveSession(sender.CurrentSession);
					}
					break;
				case Constants.AuthState.Shutdown:
					// The session should have already been saved, so we don't need to do anything here.
					break;
				default: throw new ArgumentOutOfRangeException(nameof(stateChanged), stateChanged, null);
			}
		}
	}
}

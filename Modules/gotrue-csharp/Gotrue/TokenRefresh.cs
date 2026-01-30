using System;
using System.Threading;
using Supabase.Gotrue.Interfaces;
using static Supabase.Gotrue.Constants.AuthState;

namespace Supabase.Gotrue
{
	/// <summary>
	/// Manages the auto-refresh of the Gotrue Session.
	/// </summary>
	public class TokenRefresh
	{
		private readonly Client _client;

		/// <summary>
		/// Internal timer reference for token refresh
		/// <see>
		///     <cref>AutoRefreshToken</cref>
		/// </see>
		/// </summary>
		private Timer? _refreshTimer;

		/// <summary>
		/// Turn on debug logging for the TokenRefresh
		/// </summary>
		public bool Debug;

		/// <summary>
		/// Sets up the TokenRefresh class, bound to a specific client
		/// </summary>
		/// <param name="client"></param>
		public TokenRefresh(Client client)
		{
			_client = client;
		}
		/// <summary>
		/// Turns the auto-refresh timer on or off based on the current auth state
		/// </summary>
		/// <param name="sender">The Client and Session data</param>
		/// <param name="stateChanged"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void ManageAutoRefresh(IGotrueClient<User, Session> sender, Constants.AuthState stateChanged)
		{
			switch (stateChanged)
			{
				case SignedIn:
					if (Debug)
						_client.Debug("Refresh Timer started");
					InitRefreshTimer();
					// Turn on auto-refresh timer
					break;
				case SignedOut:
					if (Debug)
						_client.Debug("Refresh Timer stopped");
					_refreshTimer?.Dispose();
					// Turn off auto-refresh timer
					break;
				case UserUpdated:
					if (Debug)
						_client.Debug("Refresh Timer restarted");
					InitRefreshTimer();
					break;
				case PasswordRecovery:
					// Doesn't affect auto refresh
					break;
				case TokenRefreshed:
					// Doesn't affect auto refresh
					break;
				case Shutdown:
					if (Debug)
						_client.Debug("Refresh Timer stopped");
					_refreshTimer?.Dispose();
					// Turn off auto-refresh timer
					break;
				default: throw new ArgumentOutOfRangeException(nameof(stateChanged), stateChanged, null);
			}
		}

		/// <summary>
		/// Sets up the auto-refresh timer
		/// </summary>
		private void InitRefreshTimer()
		{
			CreateNewTimer();
		}

		/// <summary>
		/// The timer calls this method at the configured interval to refresh the token.
		///
		/// If the user is offline, it won't try to refresh the token.
		/// </summary>
		private async void HandleRefreshTimerTick(object _)
		{
			try
			{
				if (_client.Online)
					await _client.RefreshToken();
			}
			catch (Exception ex)
			{
				// Something unusually bad happened!
				if (Debug)
					_client.Debug(ex.Message, ex);
			}
			finally
			{
				CreateNewTimer();
			}
		}

		/// <summary>
		/// Create a new refresh timer.
		/// 
		/// <para/>
		/// We pass <see cref="Timeout.InfiniteTimeSpan"/> to ensure the handler only runs once.
		/// We create a new timer after each refresh so that each refresh runs in a new thread.
		/// This keeps the refresh going if a thread crashes.
		/// Creating a thread each refresh is not so expensive when the refresh interval is an hour or longer.
		/// </summary>
		private void CreateNewTimer()
		{
			if (_client.CurrentSession == null || _client.CurrentSession.ExpiresIn == default)
			{
				if (Debug)
					_client.Debug($"No session, refresh timer not started");
				return;
			}

			if (_client.CurrentSession.Expired())
			{
				if (Debug)
					_client.Debug($"Token expired, signing out");
				_client.NotifyAuthStateChange(SignedOut);
				return;
			}

			try
			{
				TimeSpan interval = GetInterval();
				_refreshTimer?.Dispose();
				_refreshTimer = new Timer(HandleRefreshTimerTick, null, interval, Timeout.InfiniteTimeSpan);

				if (Debug)
					_client.Debug($"Refresh timer scheduled {interval.TotalMinutes} minutes");
			}
			catch (Exception e)
			{
				if (Debug)
					_client.Debug($"Failed to initialize refresh timer", e);
			}
		}

		/// <summary>
		/// Interval should be t - (1/5(n)) (i.e. if session time (t) 3600s, attempt refresh at 2880s or 720s (1/5) seconds before expiration)
		/// </summary>
		private TimeSpan GetInterval()
		{
			if (_client.CurrentSession == null || _client.CurrentSession.ExpiresIn == default)
			{
				return TimeSpan.Zero;
			}

			var interval = (long)Math.Floor(_client.CurrentSession.ExpiresIn * 4.0f / 5.0f);

			var timeoutSeconds = Convert.ToInt64((_client.CurrentSession.CreatedAt.AddSeconds(interval) - DateTime.UtcNow).TotalSeconds);

			if (timeoutSeconds > _client.Options.MaximumRefreshWaitTime)
				timeoutSeconds = _client.Options.MaximumRefreshWaitTime;

			return TimeSpan.FromSeconds(timeoutSeconds);
		}
	}
}

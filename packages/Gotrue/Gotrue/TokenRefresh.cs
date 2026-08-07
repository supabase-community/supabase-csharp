using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using Supabase.Gotrue.Exceptions;
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
					CreateNewTimer();
					// Turn on auto-refresh timer
					break;
				case SignedOut:
				case Shutdown:
					if (Debug)
						_client.Debug("Refresh Timer stopped");
					_refreshTimer?.Dispose();
					// Turn off auto-refresh timer
					break;
				case UserUpdated:
					if (Debug)
						_client.Debug("Refresh Timer restarted");
					CreateNewTimer();
					break;
				case PasswordRecovery:
				case TokenRefreshed:
				case MfaChallengeVerified:
					// Doesn't affect auto refresh
					break;
				default: throw new ArgumentOutOfRangeException(nameof(stateChanged), stateChanged, null);
			}
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
			if (_client.CurrentSession == null)
			{
				if (Debug)
					_client.Debug($"No session, refresh timer not started");
				return;
			}

			try
			{
				TimeSpan refreshDueTime = GetSecondsUntilNextRefresh();
				_refreshTimer?.Dispose();
				_refreshTimer = new Timer(HandleRefreshTimerTick, null, refreshDueTime, Timeout.InfiniteTimeSpan);

				if (Debug)
					_client.Debug($"Refresh timer scheduled {refreshDueTime.TotalMinutes} minutes");
			}
			catch (Exception e)
			{
				if (Debug)
					_client.Debug($"Failed to initialize refresh timer", e);
			}
		}

		/// <summary>
		/// Returns remaining seconds until the access token should be refreshed.
		/// Interval is calculated as:<code>t - (1/5(n))</code> (i.e. if session time (t) 3600s, attempt refresh at 2880s or 720s (1/5) seconds before expiration).
		/// <remarks>
		/// - The maximum refresh wait time is clamped to <see cref="ClientOptions.MaximumRefreshWaitTime"/>
		/// </remarks>
		/// <remarks>
		/// - If the access token is expired it will refresh immediately.
		/// </remarks>
		/// </summary>
		/// <returns>The remaining seconds until the token should be refreshed</returns>
		private TimeSpan GetSecondsUntilNextRefresh()
		{
			if (_client.CurrentSession is null || _client.CurrentSession.AccessToken == null)
			{
				return TimeSpan.Zero;
			}

			var interval = (long)Math.Floor(_client.CurrentSession.ExpiresIn * 4.0 / 5.0);
			var refreshAt = _client.CurrentSession.CreatedAt.AddSeconds(interval);

			var secondsUntilNextRefresh = Convert.ToInt64((refreshAt - DateTime.UtcNow).TotalSeconds);
			
			if (secondsUntilNextRefresh < 0)
				return TimeSpan.Zero;
			
			if (secondsUntilNextRefresh > _client.Options.MaximumRefreshWaitTime)
				secondsUntilNextRefresh = _client.Options.MaximumRefreshWaitTime;
			
			return TimeSpan.FromSeconds(secondsUntilNextRefresh);
		}
	}
}

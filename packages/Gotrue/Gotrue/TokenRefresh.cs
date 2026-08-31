using System;
using System.Threading;
using Supabase.Gotrue.Interfaces;
using static Supabase.Gotrue.Constants.AuthState;

namespace Supabase.Gotrue;

/// <summary>
/// Manages the auto-refresh of the Gotrue Session.
/// </summary>
public class TokenRefresh
{
    private readonly Client _client;

    /// <summary>
    /// Minimum wait between refresh attempts after one was skipped or failed.
    /// supabase-js polls on a fixed 30 second tick (AUTO_REFRESH_TICK_DURATION_MS) and
    /// picks a failed refresh up on the next one, so a failed attempt waits one tick too.
    /// </summary>
    private static readonly TimeSpan AutoRefreshTickDuration = TimeSpan.FromSeconds(30);

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
    public TokenRefresh(Client client) => this._client = client;
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
                if (this.Debug)
                    this._client.Debug("Refresh Timer started");
                this.CreateNewTimer();
                // Turn on auto-refresh timer
                break;
            case SignedOut:
            case Shutdown:
                if (this.Debug)
                    this._client.Debug("Refresh Timer stopped");
                this._refreshTimer?.Dispose();
                // Turn off auto-refresh timer
                break;
            case UserUpdated:
            case TokenRefreshed:
                if (this.Debug)
                    this._client.Debug("Refresh Timer restarted");
                this.CreateNewTimer();
                break;
            case PasswordRecovery:
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
        var refreshCompleted = false;
        try
        {
            if (this._client.Online)
            {
                await this._client.RefreshToken();
                refreshCompleted = true;
            }
        }
        catch (Exception ex)
        {
            // Something unusually bad happened!
            if (this.Debug)
                this._client.Debug(ex.Message, ex);
        }
        finally
        {
            // An expired session schedules at zero, so a refresh that was skipped or
            // failed must wait a tick before the next attempt instead of hot-looping.
            this.CreateNewTimer(refreshCompleted ? TimeSpan.Zero : AutoRefreshTickDuration);
        }
    }

    /// <summary>
    /// Create a new refresh timer.
    /// 
    /// <para/>
    /// We pass <see cref="Timeout.InfiniteTimeSpan"/> to ensure the handler only runs once.
    /// We create a new timer after each refresh instead of a repeating one, so the next tick is
    /// scheduled from the session we ended up with rather than from a fixed interval.
    /// The callbacks run on the thread pool, so a timer per refresh is cheap at this cadence.
    /// </summary>
    private void CreateNewTimer(TimeSpan minimumDelay = default)
    {
        if (this._client.CurrentSession == null)
        {
            if (this.Debug)
                this._client.Debug($"No session, refresh timer not started");
            return;
        }

        try
        {
            var refreshDueTime = this.GetSecondsUntilNextRefresh();
            if (refreshDueTime < minimumDelay)
                refreshDueTime = minimumDelay;
            this._refreshTimer?.Dispose();
            this._refreshTimer = new Timer(this.HandleRefreshTimerTick, null, refreshDueTime, Timeout.InfiniteTimeSpan);

            if (this.Debug)
                this._client.Debug($"Refresh timer scheduled {refreshDueTime.TotalMinutes} minutes");
        }
        catch (Exception e)
        {
            if (this.Debug)
                this._client.Debug($"Failed to initialize refresh timer", e);
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
        if (this._client.CurrentSession is null || this._client.CurrentSession.AccessToken == null)
        {
            return TimeSpan.Zero;
        }

        var interval = (long) Math.Floor(this._client.CurrentSession.ExpiresIn * 4.0 / 5.0);
        var refreshAt = this._client.CurrentSession.CreatedAt.AddSeconds(interval);

        var secondsUntilNextRefresh = Convert.ToInt64((refreshAt - DateTime.UtcNow).TotalSeconds);

        if (secondsUntilNextRefresh < 0)
            return TimeSpan.Zero;

        if (secondsUntilNextRefresh > this._client.Options.MaximumRefreshWaitTime)
            secondsUntilNextRefresh = this._client.Options.MaximumRefreshWaitTime;

        return TimeSpan.FromSeconds(secondsUntilNextRefresh);
    }
}

# Updated Refresh Token Handling

The Supabase client supports setting a maximum wait time before refreshing the token. This is useful
for scenarios where you want to refresh the token before it expires, but not too often.

By default, GoTrue servers are typically set to expire the token after an hour, and the refresh
thread will refresh the token when ~20% of that time is left.

However, you can set the expiration time to be much longer on the server (up to a week). In this
scenario, you may want to refresh the token more often than once every 5 days or so, but not every hour.

There is now a new option `MaximumRefreshWaitTime` which allows you to specify the maximum amount
in time that the refresh thread will wait before refreshing the token. This defaults to 4 hours.
This means that if you have your server set to a one hour token expiration, nothing changes, but
if you extend the server refresh to (for example) a week, as long as the user launches the app
at least once a week, they will never have to re-authenticate.

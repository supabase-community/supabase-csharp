# Session Persistence

## Persisting, Retrieving, and Destroying Sessions.

This Gotrue client is written to be agnostic when it comes to session persistence, retrieval, and
destruction. `ClientOptions` exposes properties that allow these to be specified.

In the event these are specified and the `AutoRefreshToken` option is set, as the `Client` Initializes, it will also
attempt to retrieve, set, and refresh an existing session.

# Xamarin Example

Using `Xamarin.Essentials` in `Xamarin.Forms`, this might look like:

```c#
// This is a method you add your application launch/setup
async void Initialize() {

    // Specify the methods you'd like to use as persistence callbacks
    var persistence = new GotrueSessionPersistence(SaveSession, LoadSession, DestroySession);
    var client = new Client(
            Url = GOTRUE_URL,
            new ClientOptions { 
                AllowUnconfirmedUserSessions = true, 
                SessionPersistence = persistence });
                
    // Specify a debug callback to listen to problems with the background token refresh thread
    client.AddDebugListener(LogDebug);
    
    // Specify a call back to listen to changes in the user state (logged in, out, etc)
    client.AddStateChangedListener(AuthStateListener);

    // Load the session from persistence
    client.LoadSession();
    // Loads the session using SessionRetriever and sets state internally.
    await client.RetrieveSessionAsync();
}

// Add callback methods for above
// Here's a quick example of using this to save session data to the user's cache folder
// You'll want to add methods for loading the file and deleting when the user logs out 
internal bool SaveSession(Session session)
{
    var cacheFileName = ".gotrue.cache";
    
    try
    {
        var cacheDir = FileSystem.CacheDirectory;
        var path = Path.Join(cacheDir, cacheFileName);
        var str = JsonConvert.SerializeObject(session);

        using (StreamWriter file = new StreamWriter(path))
        {
            file.Write(str);
            file.Dispose();
            return Task.FromResult(true);
        };
    }
    catch (Exception err)
    {
        Debug.WriteLine("Unable to write cache file.");
        throw err;
    }
}
```

You can find other sample implementations in the [Unity](Unity.md)
and [Desktop Clients](DesktopClients.md) documentation.

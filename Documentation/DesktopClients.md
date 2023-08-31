# Desktop Clients

A desktop client for the purposes of this document is a client that runs on an
end user machine. For example, a Unity, Godot, or MonoGame game, or an application built with
Xamarin, MAUI or Avalonia app.

There are a LOT of potential desktop clients, and many of them have very specific rules for
things like UI thread access, or how to handle file paths. This document will attempt to
cover the most common scenarios, but it is not exhaustive.

This library includes the core library for accessing and using Supabase services, as well
utilities and interfaces to make it easier to use the library in common desktop scenarios.
For example, clients often go offline, and then come back online. Different UI frameworks
have very different UI threading models, which affects how you might need to deal with callbacks.
And, of course, because pretty much everything involves async/await calls, you need to be
comfortable with
the [.NET async system](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/async).

So, while this library is intended to be as easy to use as possible, it does require some
understanding of the underlying .NET system and (depending on your choice of UI framework)
may require some adaption. In most cases, this means implementing an existing interface
with the specific details required by your UI framework.

Typical areas where you may need to adapt the library include:

- [Session Persistence](SessionPersistence.md) - basically, where to store the JWT token (usually on disk)
  so the user doesn't have to log in again every time they start the app.
- [Network Awareness/Offline Support](OfflineSupport.md) - updating the client to handle
  network connectivity changes and to handle offline operations.

## Brief Note on .NET Versions

There are many different [versions of .NET](https://versionsof.net/), and the naming can be
somewhat confusing. In addition to the different versions, there's also the .NET Standard
set of specifications.

This project targets
the [.NET Standard 2.0 specification](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0).
This means that it can be used with a wide variety of different client-side technologies,
which you can see looking at the .NET Standard
2.0 [compatibility matrix](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#net-implementation-support).

## Installation

The [library is available via NuGet](https://www.nuget.org/packages/supabase-csharp). Depending on your
environment, you can install it via the command line:

```
dotnet add package supabase-csharp
```

Many IDEs also provide a way to install NuGet packages. If you are using Unity you can check out
the [Unity specific instructions](Unity.md).

### Projects defaulting to `System.Text.Json` (i.e. Blazor WASM)

You will need to manuall install NewtonsoftJson support:

```bash
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson --version 7.0.5
```
And include the following in your initialization code:
```c#
builder.Services.AddControllers().AddNewtonsoftJson();
````

## Getting Started

From a high level, for most desktop or mobile apps you create a Supabase client as long-running
object, configure it, and then use it to access the various services.

Below is a heavily annotated example based on the Unity, but this can be adapted to other
UI frameworks.

```csharp
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Client = Supabase.Client;

private async void Initialize()
{
	// We create an object to monitor the network status.
    NetworkStatus NetworkStatus = new();

    // Create a Supabase objects object.
    SupabaseOptions options = new();
    // Set the options to auto-refresh the JWT token with a background thread.
    options.AutoRefreshToken = true;

    // Create the client object. Note that the project URL and the anon key are
    // available in the Supabase dashboard. In addition, note that the public anon
    // key is not a security risk - in JavaScript projects, this key is visible
    // in the browser viewing source!
    _supabase = new Client("https://project URL", "supabase PUBLIC anon key", options);
    
    // This adds a listener for debug information, especially useful for dealing
    // with errors from the auto-refresh background thread.
    _supabase.Auth.AddDebugListener(DebugListener);

    // Here we are getting the auth service and passing it to the network status
    // object. The network status object will tell the auth service when the
    // network is up or down.
    NetworkStatus.Client = (Supabase.Gotrue.Client)_supabase.Auth;

    // Here we are setting up the persistence layer. This is an object that implements
    // the IGotrueSessionPersistence<Session> interface. This is used to store the JWT 
    // token so the user won't have to log in every time they start the app.
    _supabase.Auth.SetPersistence(new UnitySession());
    
    // Here we are setting up the listener for the auth state. This listener will
    // be called in response to the auth state changing. This is where you would
    // update your UI to reflect the current auth state.
    _supabase.Auth.AddStateChangedListener(UnityAuthListener);
    
    // We now instruct the auth service to load the session from disk using the persistence
    // object we created earlier
    _supabase.Auth.LoadSession();
    
    // In this case, we are setting up the auth service to allow unconfirmed user sessions.
    // Depending on your use case, you may want to set this to false and require the user
    // to validate their email address before they can log in.
    _supabase.Auth.Options.AllowUnconfirmedUserSessions = true;

    // This is a well-known URL that is used to test network connectivity.
    // We use this to determine if the network is up or down.
    string url =
        $"{SupabaseSettings.SupabaseURL}/auth/v1/settings?apikey={SupabaseSettings.SupabaseAnonKey}";
    try
    {
        // We start the network status object. This will attempt to connect to the
        // well-known URL and determine if the network is up or down.
        _supabase!.Auth.Online = await NetworkStatus.StartAsync(url);
    }
    catch (NotSupportedException)
    {
        // On certain platforms, the NetworkStatus object may not be able to determine
        // the network status. In this case, we just assume the network is up.
        _supabase!.Auth.Online = true;
    }
    catch (Exception e)
    {
        // If there are other kinds of error, we assume the network is down,
        // and in this case we send the error to a UI element to display to the user.
        // This PostMessage method is specific to this application - you will
        // need to adapt this to your own application.
        PostMessage(NotificationType.Debug, $"Network Error {e.GetType()}", e);
        _supabase!.Auth.Online = false;
    }
    if (_supabase.Auth.Online)
    {
        // If the network is up, we initialize the Supabase client.
        await _supabase.InitializeAsync();
        
        // Here we are fetching the current settings for the auth service as exposed
        // by the server. For example, we might want to know which providers have been
        // configured, or change the behavior if auto-confirm email is turned off or on.
        Settings = await _supabase.Auth.Settings();
    }
}
```

## More Examples

- [Phantom-KNA](https://gist.github.com/Phantom-KNA)
  posted this example of [a Xamarin Forms/MAUI](https://gist.github.com/Phantom-KNA/0eabbbe52076370489d0ecbf73f0a6c6)
  bootstrap/configuration.
- [kaushalkumar86](https://github.com/kaushalkumar86) posted this 
example of how to implement the [Realtime notification listeners](https://github.com/supabase-community/realtime-csharp/issues/34#issuecomment-1696985179)
- [salsa2k](https://github.com/salsa2k) posted this example of working [auth using MAUI Blazor Hybrid](https://github.com/supabase-community/supabase-csharp/discussions/83#discussioncomment-6863545)

## Next Steps

- Read the [Supabase documentation](https://supabase.com/docs), watch videos, etc.
- Browse the [API documentation](https://supabase-community.github.io/supabase-csharp/api/Supabase.html) for the
  Supabase C# client.
- Check out the Examples and Tests for each of the sub-project libraries.
- Check out the [Supabase C# discussion board](https://github.com/supabase-community/supabase-csharp/discussions)
- Have fun!

<p align="center">
<img width="300" src=".github/supabase-csharp.png"/>
</p>
<p align="center">
  <img src="https://github.com/supabase/supabase-csharp/workflows/Build%20And%20Test/badge.svg"/>
  <a href="https://www.nuget.org/packages/supabase-csharp/">
    <img src="https://img.shields.io/nuget/vpre/supabase-csharp"/>
  </a>
</p>

Documentation can be found [below](#getting-started), on the [Supabase Developer Documentation](https://supabase.com/docs/reference/csharp/introduction) and additionally in the [Generated API Docs](https://supabase-community.github.io/supabase-csharp/api/Supabase.Client.html).

## Status

- [x] Integration with [Supabase.Realtime](https://github.com/supabase-community/realtime-csharp)
- [x] Integration with [Postgrest](https://github.com/supabase-community/postgrest-csharp)
- [x] Integration with [Gotrue](https://github.com/supabase-community/gotrue-csharp)
- [x] Integration with [Supabase Storage](https://github.com/supabase-community/storage-csharp)
- [x] Integration with [Supabase Edge Functions](https://github.com/supabase-community/functions-csharp)
- [x] [Nuget Release](https://www.nuget.org/packages/supabase-csharp)

## Projects / Examples / Templates

- Supabase + Unity + Apple Native Sign-in - [Video](https://www.youtube.com/watch?v=S0hTwtsUWcM) by [@wiverson](https://github.com/wiverson)
- Blazor WASM Template using Supabase - [Repo](/Examples/BlazorWebAssemblySupabaseTemplate) / [Live demo](https://blazorwasmsupabasetemplate.web.app/) by [@rhuanbarros](https://github.com/rhuanbarros)
- Realtime Example using Supabase Realtime Presence - [Repo](https://github.com/supabase-community/realtime-csharp/tree/master/Examples/PresenceExample) / [Live demo](https://multiplayer-csharp.azurewebsites.net/)

(Create a PR to list your work here!)

## Getting Started

If you prefer video format: [@Milan Jovanović](https://www.youtube.com/@MilanJovanovicTech) has created a [video crash course to get started](https://www.youtube.com/watch?v=uviVTDtYeeE)!

Care has been taken to mirror, as much as possible, the [Javascript Supabase API](https://github.com/supabase/supabase-js). As this is an unofficial client, there are times where this client lags behind the offical client. **If there are missing features, please open an issue or pull request!**

1. To get started, create a new project in the [Supabase Admin Panel](https://app.supabase.io).
2. Grab your Supabase URL and Supabase Public Key from the Admin Panel (Settings -> API Keys).
3. Initialize the client!

_Note: `supabase-csharp` has some APIs that require the `service_key` rather than the `public_key` (for instance: the administration of users, bypassing database roles, etc.). If you are using the `service_key` **be sure it is not exposed client side.** Additionally, if you need to use both a service account and a public/user account, please do so using a separate client instance for each._

#### Note for Projects defaulting to `System.Text.Json` (i.e. Blazor WASM):
You will need to install NewtonsoftJson support:
```bash
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson --version 7.0.5
```
And include the following in your initialization code:
```c#
builder.Services.AddControllers().AddNewtonsoftJson();
````

### Initializing a Client

Initializing a barebones client is pretty simple.

```c#
var supabase = new Supabase.Client(SUPABASE_URL, SUPABASE_KEY);
await supabase.InitializeAsync();
```

Or, using options:

```c#
var options = new SupabaseOptions
{
    AutoConnectRealtime = true
};
var supabase = new Supabase.Client(SUPABASE_URL, SUPABASE_KEY, options);

// Calling InitializeAsync will automatically attempt a socket connection if specified in the options.
await supabase.InitializeAsync();
```

### Using the Client

As for actually using the client, each service is listed as a property on `Supabase.Client`. Some services have helpers to make interactions easier. Properties are provided for every client in the event that advanced customization of the client is needed.

1. `Supabase.Postgrest`
   - Is better accessed using `supabase.From<ModelName>()` as it provides a wrapper class with some helpful accessors (see below)
2. `Supabase.Realtime`
   - If used for listening to `postgres_changes` can be accessed using: `supabase.From<ModelName>().On(listenerType, (sender, response) => {})`
   - Otherwise, use `Supabase.Realtime.Channel("channel_name")` for `Broadcast` and `Presence` listeners.

```c#
// Get the Auth Client
var auth = supabase.Auth;

// Get the Postgrest Client for a Model
var table = supabase.From<TModel>();

// Invoke an RPC Call
await supabase.Rpc("hello_world", null);

// Invoke a Supabase Function
await supabase.Functions.Invoke("custom_function");

// Get the Storage Client
var storageBucket = supabase.Storage.From("bucket_name");

// Use syntax for broadcast, presence, and postgres_changes
var realtime = supabase.Realtime.Channel("room_1");

// Alternatively, shortcut syntax for postgres_changes
await supabase.From<TModel>().On(ListenType.All, (sender, response) =>
{
    switch (response.Event)
    {
        case Constants.EventType.Insert:
            break;
        case Constants.EventType.Update:
            break;
        case Constants.EventType.Delete:
            break;
    }

    Debug.WriteLine($"[{response.Event}]:{response.Topic}:{response.Payload.Data}");
});
```

**Notes**

- Be aware that many of the supabase features require permissions for proper access from a client. This is **especially true** for `realtime`, `postgres`, and `storage`. If you are having problems getting the client to pull data, **verify that you have proper permissions for the logged in user.**
- Connection to `Supabase.Realtime` is, by default, not enabled automatically, this can be changed in options.
- When logging in using the `Supabase.Auth` (Gotrue) client, state is managed internally. The currently logged in user's token will be passed to all the Supabase features automatically (via header injection).
- Token refresh enabled by default and is handled by a timer on the Gotrue client.
- Client libraries [listed above](#status) have additional information in their readme files.

Of course, there are options available to customize the client. Which can be found in `Supabase.SupabaseOptions`.

### Initializing a Client (with Gotrue/Auth Session Persistence)

You can specify a session handler to persist user sessions in your app. For example:

`CustomSessionHandler.cs`

```c#
class CustomSessionHandler : IGotrueSessionPersistence<Session>
{
    public void SaveSession(Session session)
    {
        // Persist Session in Filesystem or in browser storage
        // JsonConvert.SerializeObject(session) will be helpful here!
        throw new NotImplementedException();
    }

    public void DestroySession()
    {
        // Destroy Session on Filesystem or in browser storage
        throw new NotImplementedException();
    }

    public Session LoadSession()
    {
        // Retrieve Session from Filesystem or from browser storage
        // JsonConvert.DeserializeObject<TSession>(value) will be helpful here!
        throw new NotImplementedException();
    }
}
```

Then initialize using the specified handler:

`Main.cs`

```c#
var options = new SupabaseOptions
{
    // ....
    SessionHandler = new CustomSessionHandler()
};

var supabase = new Supabase.Client(SUPABASE_URL, SUPABASE_KEY, options);

// Calling InitializeAsync will automatically invoke the SessionHandler to setup the internal session state
await supabase.InitializeAsync();
```

## Package made possible through the efforts of:

Join the ranks! See a problem? Help fix it!

<a href="https://github.com/supabase-community/supabase-csharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=supabase-community/supabase-csharp" />
</a>

Made with [contrib.rocks](https://contrib.rocks/preview?repo=supabase-community%2Fsupabase-csharp).

## Contributing

We are more than happy to have contributions! Please submit a PR.

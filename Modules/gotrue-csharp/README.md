<p align="center">
<img width="300" src=".github/supabase-gotrue.png"/>
</p>

<p align="center">
  <img src="https://github.com/supabase/gotrue-csharp/workflows/Build%20And%20Test/badge.svg"/>
  <a href="https://www.nuget.org/packages/Supabase.Gotrue/">
    <img src="https://img.shields.io/nuget/vpre/Supabase.Gotrue"/>
  </a>
</p>

---

## [Notice]: v5.0.0 renames this package from `gotrue-csharp` to `Supabase.Gotrue`. The depreciation notice has been set in NuGet. The API remains the same.

## New Features

### Unity Support

The Client works with Unity. You can find an example of a session persistence
implementation for Unity at this [gist](https://gist.github.com/wiverson/fbb07498743dff19b72c9c58599931e9).

```csharp

```

### Offline Support

The Client now better supports online/offline usage. The Client now has a simple boolean option "Online"
which can be set to to false. This can be combined with the NetworkStatus class to allow the client
to automatically go online & offline based on the device's network status.

To use this new NetworkStatus, add the following:

```csharp
// Create the client
var client = new Client(new ClientOptions { AllowUnconfirmedUserSessions = true });
// Create the network status monitor
var status = new NetworkStatus();
// Tell the network status monitor to update the client's online status
status.Client = client;
// Start the network status monitor
await status.StartAsync();
// rest of the usual client configuration
```

Only the stateful Client supports this feature, and only for the managed user sessions.
Admin JWT methods and the stateless client are not affected.

By default, this change will not affect existing code.

### Updated Refresh Token Handling

The Client now supports setting a maximum wait time before refreshing the token. This is useful
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

## BREAKING CHANGES v3.1 → v4.x

- Exceptions have been simplified to a single `GotrueException`. A `Reason` field has been added
  to `GotrueException` to clarify what happened. This should also be easier to manage as the Gotrue
  server API & messages evolve.
- The session delegates for `Save`/`Load`/`Destroy` have been simplified to no longer require `async`.
- Console logging in a few places (most notable the background refresh thread) has been removed
  in favor of a notification method. See `Client.AddDebugListener()` and the test cases for examples.
  This will allow you to implement your own logging strategy (write to temp file, console, user visible
  err console, etc).
- The client now more reliably emits AuthState changes.
- There is now a single source of truth for headers in the stateful Client - the `Options` headers.

New feature:

- Added a `Settings` request to the stateless API only - you can now query the server instance to
  determine if it's got the settings you need. This might allow for things like a visual
  component in a tool to verify the GoTrue settings are working correctly, or tests that run differently
  depending on the server configuration.

Implementation notes:

- Test cases have been added to help ensure reliability of auth state change notifications
  and persistence.
- Persistence is now managed via the same notifications as auth state change

## BREAKING CHANGES v3.0 → 3.1

- We've implemented the PKCE auth flow. SignIn using a provider now returns an instance of `ProviderAuthState` rather
  than a `string`.
- The provider sign in signature has moved `scopes` into `SignInOptions`

In Short:

```c#
# What was:
var url = await client.SignIn(Provider.Github, "scopes and things");

# Becomes:
var state = await client.SignIn(Provider.Github, new SignInOptions { "scopes and things" });
// Url is now at `state.Uri`
```

---

## Getting Started

To use this library on the Supabase Hosted service but separately from the `supabase-csharp`, you'll need to specify
your url and public key like so:

```c#
var auth = new Supabase.Gotrue.Client(new ClientOptions<Session>
{
    Url = "https://PROJECT_ID.supabase.co/auth/v1",
    Headers = new Dictionary<string, string>
    {
        { "apikey", SUPABASE_PUBLIC_KEY }
    }
})
```

Otherwise, using it this library with a local instance:

```c#
var options = new ClientOptions { Url = "https://example.com/api" };
var client = new Client(options);
var user = await client.SignUp("new-user@example.com");

// Alternatively, you can use a StatelessClient and do API interactions that way
var options = new StatelessClientOptions { Url = "https://example.com/api" }
await new StatelessClient().SignUp("new-user@example.com", options);
```

## Persisting, Retrieving, and Destroying Sessions.

This Gotrue client is written to be agnostic when it comes to session persistence, retrieval, and
destruction. `ClientOptions` exposes
properties that allow these to be specified.

In the event these are specified and the `AutoRefreshToken` option is set, as the `Client` Initializes, it will also
attempt to
retrieve, set, and refresh an existing session.

For example, using `Xamarin.Essentials` in `Xamarin.Forms`, this might look like:

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

## 3rd Party OAuth

Once again, Gotrue client is written to be agnostic of platform. In order for Gotrue to sign in a user from an Oauth
callback, the PKCE flow is preferred:

1) The Callback Url must be set in the Supabase Admin panel
2) The Application should have listener to receive that Callback
3) Generate a sign in request using: `client.SignIn(PROVIDER, options)` and setting the options to use the
   PKCE `FlowType`
4) Store `ProviderAuthState.PKCEVerifier` so that the application callback can use it to verify the returned code
5) In the Callback, use stored `PKCEVerifier` and received `code` to exchange for a session.

```c#
var state = await client.SignIn(Constants.Provider.Github, new SignInOptions
{
    FlowType = Constants.OAuthFlowType.PKCE,
    RedirectTo = "http://localhost:3000/oauth/callback"
});

// In callback received from Supabase returning to RedirectTo (set above)
// Url is set as: http://REDIRECT_TO_URL?code=CODE
var session = await client.ExchangeCodeForSession(state.PKCEVerifier, RETRIEVE_CODE_FROM_GET_PARAMS);
```

## Sign In With Single Sign On (SSO)
Single Sign On (SSO) is an enterprise level authentication protocol that allows a single enterprise account to
access many apps at once. A few examples of supported SSO providers are Okta, Microsoft Entra and Google Workspaces

If not already done so, you must first add an SSO provider to your supabase project via the supabase CLI.
See the following [link](https://supabase.com/docs/guides/auth/enterprise-sso/auth-sso-saml) for more info on how to
configure SSO providers

The flow functions similar to the OAuth flow and supports many of the same parameters. Under the hood
the flow is handled quite differently by the GoTrue server but the client is agnostic to the difference in 
implementations and session info is handled in the same way as the OAuth flow

General auth flow is as follows:

1. Request initiated by calling `SignInWithSSO`
   1. The `RedirectTo` attribute is recommended for handling the callback and converting to a session
1. `ssoResposne` contains the providers login Uri, navigate to this
1. User logs in with provider (Okta/Auth0, Microsoft Entra, Google Workspaces ect...)
1. Supabase GoTrue server handles SAML exchange for us
   1. Supabase GoTrue server generates session info and appends it to the callback (RedirectedTo) url
1. We can then use either `ExchangeCodeForSession(code)` or `GetSessionFromUrl(callbackUri)`

```csharp
using Constants = Supabase.Gotrue.Constants;

var ssoResponse = await client.SignInWithSSO("supabase.io", new SignInWithSSOOptions
{
    RedirectTo = "https://localhost:3000/welcome"
});

// Handle login via ssoResponse.Uri
//
// When the user logs in using the Uri from the ssoResponse, 
// they will be redirected to the RedirectTo

// In callback received from Supabase returning to RedirectTo (set above)
// Url is set as: http://REDIRECT_TO_URL?access_token=foobar&expires_at=123...
var session = await client.GetSessionFromUrl(url);
```

For handling session persistence its recommended using a session persistence layer, take a look at
the following [example](GotrueExample/SupabaseClientPersistence.cs)

For additional info on how the GoTrue server handles SSO requests see 
[here](https://github.com/supabase/auth/blob/55409f797bea55068a3fafdddd6cfdb78feba1b4/internal/api/samlacs.go#L315-L316)
and [here](https://github.com/supabase/auth/blob/55409f797bea55068a3fafdddd6cfdb78feba1b4/internal/api/token.go#L54-L55)
## Troubleshooting

**Q: I've created a User but while attempting to log in it throws an exception:**

A: Provided the credentials are correct, make sure that the User has also confirmed their email.

Adding a handler for email confirmation to a desktop or mobile application can be done, but it
requires setting up URL handlers for each platform, which can be pretty difficult to do if you
aren't really comfortable with configuring these handlers. (
e.g. [Windows](https://learn.microsoft.com/en-us/windows/win32/search/-search-3x-wds-ph-install-registration),
[Apple](https://developer.apple.com/documentation/xcode/defining-a-custom-url-scheme-for-your-app),
[Android](https://developer.android.com/training/app-links))
You may find it easier to create a
simple web application to handle email confirmation - that way a user can just click a link in
their email and get confirmed that way. Your desktop or mobile app should inspect the user object
that comes back and use that to see if the user is confirmed.

You might find it easiest to do something like create and deploy a
simple [SvelteKit](https://kit.svelte.dev/) or even a very basic
pure [JavaScript](https://github.com/supabase/examples-archive/tree/main/supabase-js-v1/auth/javascript-auth) project
to handle email verification.

## Status

- [x] API
    - [x] Sign Up with Email
    - [x] Sign In with Email
    - [x] Send Magic Link Email
    - [x] Invite User by Email
    - [x] Reset Password for Email
    - [x] Signout
    - [x] Get Url for Provider
    - [x] Get User
    - [x] Update User
    - [x] Refresh Access Token
    - [x] List Users (includes filtering, sorting, pagination)
    - [x] Get User by Id
    - [x] Create User
    - [x] Update User by Id
    - [x] Sign In with Single Sign On (SSO)
- [x] Client
    - [x] Get User
    - [x] Refresh Session
    - [x] Auth State Change Handler
    - [x] Provider Sign In (Provides URL)
    - [x] Sign In with Single Sign On (SSO)
- [x] Provide Interfaces for Custom Token Persistence Functionality
- [x] Documentation
- [x] Unit Tests
- [x] Nuget Release

## Package made possible through the efforts of:

Join the ranks! See a problem? Help fix it!

<a href="https://github.com/supabase-community/gotrue-csharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=supabase-community/gotrue-csharp" />
</a>

<small>Made with [contrib.rocks](https://contrib.rocks).</small>

## Contributing

We are more than happy to have contributions! Please submit a PR.

### Testing

To run the tests locally you must have docker and docker-compose installed. Then in the root of the repository run:

- `docker-compose up -d`
- `dotnet test`

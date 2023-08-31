# 3rd Party OAuth

OAuth is a standard for allowing users to sign in to your application using an account
from another provider. This is great, because it allows Supabase to support
a wide range of social media providers. Unfortunately, most documentation for
OAuth involves a purely web-based flow, which doesn't work well for native
applications.

The workaround for this challenge is to use some kind of deep-linking mechanism.
All of the major OS providers include a mechanism for allowing an native application
to declare that it can handle a particular URL scheme. In this flow, the application
will open a web browser to the OAuth provider's web site, and the web site will
redirect back to the application using the URL scheme. The application can then
handle the callback and complete the OAuth flow.

You'll have to check the documentation for your particular OAuth provider, your
target framework[s], and your target platform[s] to figure out how to do set up this
entire flow. Superficially, the flow is:

1. The user clicks a social login button
2. The application opens a web browser to the OAuth provider's web site with a
   callback URL that uses the application's URL scheme
3. The user logs in to the OAuth provider
4. The OAuth provider redirects back to the application using the callback URL
5. The application handles the callback and sends the details back to Supabase.
6. Supabase converts the OAuth token to a Supabase session.
7. The application uses the Supabase session to authenticate the user.

Unfortunately, the details of this flow are different for every OAuth provider,
every target framework, and every target platform. You'll have to do some research
to figure out how to do this for your particular application.

Below is an example of how to do this for Google Sign In, but the details will
be different for every provider.

## Supabase OAuth

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

## Additional Example

Note: Make sure Project Settings -> Auth -> Auth settings -> User Signups is turned ON.

After your implementation with [GoogleSignInClient](https://github.com/googlesamples/google-signin-unity) or another,
use the IdToken like this:

```csharp
var identityToken = Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(GoogleIdToken), 0, GoogleIdToken.Length);
```

And finally use SignInWithIdToken for Login or Create New User:

```csharp
var user = await Supabase.Auth.SignInWithIdToken(Supabase.Gotrue.Constants.Provider.Google, identityToken);
```

Thanks to [Phantom-KNA](https://gist.github.com/Phantom-KNA) for this example.
# Native Sign In With Apple

Apple has recently taken more aggressive steps toward requiring developers to
use the native Sign in with Apple flow on submitted applications. In particular,
while you can use an OAuth flow for Sign in with Apple on web platforms, many
developers have reported issues with Apple rejecting applications that use
this flow on iOS.

To support Sign in with Apple, first you need to configure the flow with Apple
and Supabase. You can find instructions for this in the [Supabase documentation on
Apple Sign in](https://supabase.com/docs/guides/auth/social-login/auth-apple).
Be sure to follow the steps _very_ carefully, as a single typo can cause the
whole thing to not work.

Tip: It may seem confusing to have to configure the flow for native Sign in with Apple
"twice" - once for the OAuth web flow and once for native mobile/desktop flow.
This is because the native flow gets tokens back from Apple, and then uses the
web flow to complete the validation process.

Once all of the server configuration is done, you just need to configure a
few options for the native dialog, call it to present it to the user, and then 
handle the callback. Once you have the information from the dialog (a JWT 
from Apple), you then send that JWT to Supabase to convert it to a Supabase session.

Tip: You have to make sure everything is all configured correction - the 
application bundle ID, the nonce, etc. If you get any of these wrong, the
dialog will not work and the error message may or may not be very helpful.
You may want to include some kind of debug logging to help you figure out
what's going on in a way that you can monitor even in a non-debug build.
This might be as simple as writing to a user-accessible file or perhaps
involving presenting a debug console in the UI in some fasion.

## Native Sign in with Apple via Unity

You can use native Sign in with Apple functionality by [installing a plugin](https://github.com/lupidan/apple-signin-unity).

Depending on the platform you are using, you may need to write a bit of Swift code to expose
the native dialog, or you may be able to find an existing library. For example, here is
information on using native [Sign in with Apple on Xamarin](https://learn.microsoft.com/en-us/xamarin/ios/platform/ios13/sign-in).

Here is a snippet of pseudo-C# code to show how to use the native dialog:

```csharp

// These are values that we will use to handle the flow.

// This is a one-time use code. It's used to verify the JWT from Apple.
private string? _nonce;

// This is a hash of the one-time use code.
private string? _nonceVerify;

// This is the identity token from Apple.
private string? _identityToken;

// This is the authorization code from Apple.
private string? _authorizationCode;

// This method brings up the native dialog.
public void SignInWithApple()
{
    // This generates a random nonce (a one-time code) used to verify the JWT from
    // Apple.
    _nonce = Supabase.Gotrue.Helpers.GenerateNonce();
    
    // This is a SHA256 hash of the nonce.
    _nonceVerify = Supabase.Gotrue.Helpers.GenerateSHA256NonceFromRawNonce(_nonce);

    // Here we set the options for the native dialog, including the nonce.
    AppleAuthLoginArgs loginArgs =
        new(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, _nonceVerify);

    // Here we are asking the native dialog wrapper library to present the native
    // dialog. We are passing in the nonce and a callback to handle the results.
    _appleAuthManager!.LoginWithAppleId(loginArgs, SuccessCallback, ErrorCallback);
}

// This method handles the callback from the native dialog and sets the data
// to be used later.
private void SuccessCallback(ICredential credential)
{
    // Obtained credential, cast it to IAppleIDCredential
    if (credential is IAppleIDCredential appleIdCredential)
    {
        // Apple User ID
        string? userId = appleIdCredential.User;

        // Email and first name are received ONLY in the first login
        // You may want to add logic to save these fields locally

        // Identity token
        _identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0,
            appleIdCredential.IdentityToken.Length);

        // Authorization code
        _authorizationCode = Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode, 0,
            appleIdCredential.AuthorizationCode.Length);

        // And now you have all the information to create/login a user in your system
        _doAppleSignIn = true;
    }
    else
    {
        // Error handling - you don't want to use Sign in with Apple on non Apple devices
    }
}

// Now we use an Update() declared async.
private async void Update()
{
    // Updates the AppleAuthManager instance to execute
    // pending callbacks inside Unity's execution loop
    _appleAuthManager?.Update();

    if (_doAppleSignIn)
    {
        _doAppleSignIn = false;
        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
        bool status = await SignInToSupabaseWithApple();
        if (!status)
            Debug.Log("Sign in failed", gameObject);
        _doAppleSignIn = false;
    }
}

// This method takes the data from the callback and sends it to Supabase to
// validate the JWT and create a Supabase session. Note that because this method
// goes over the network, it's declared async. In Unity this means you might want
// to call this method from an Update method marked async.
private async Task<bool> SignInToSupabaseWithApple()
{
    try
    {
        Session? session = await SupabaseManager.Client()?.Auth.SignInWithIdToken(Constants.Provider.Apple, _identityToken!, _nonce)!;
        if (session?.User?.Id != null)
        {
            // You logged in successfully! Depending on your application you may want to load another scene, etc.
        }
        else
        {
            // Something went wrong - you may want to display an error message to the user.
        }
    }
    catch (GotrueException e)
    {
        // Something went wrong - you may want to display an error message to the user. GotrueExceptions
        // generally mean the server sent back an error message.
    }
    catch (Exception e)
    {
        // Catch-all for anything else that might have gone wrong.
    }
    return true;
}

```
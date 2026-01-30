# Changelog

## 6.0.3 - 2024-07-26

- [Re: 105](https://github.com/supabase-community/gotrue-csharp/pull/105) Add admin calls for MFA. Big thanks
  to [@michaelschattgen](https://github.com/michaelschattgen).

## 6.0.2 - 2024-07-25

- [Re: 103](https://github.com/supabase-community/gotrue-csharp/pull/103) Add support for MFA signup and login flows.
  Huge thanks to [@michaelschattgen](https://github.com/michaelschattgen) for this implementation!

## 6.0.1 - 2024-07-19

- [Re: #102](https://github.com/supabase-community/gotrue-csharp/pull/102) Add ExchangeCodeForSession to
  StatelessClient.
  Thanks [@alexbakker](https://github.com/alexbakker)!

## 6.0.0 - 2024-07-14

- Major: Change to targeting framework to `netstandard2.1`
    - [Re: #99](https://github.com/supabase-community/gotrue-csharp/pull/99) Use a CSPRNG to generate the code verifier.
      Thanks [@alexbakker](https://github.com/alexbakker)!
- [Re: #101](https://github.com/supabase-community/gotrue-csharp/pull/101) Ban user functionality.
  Thanks [@celestebyte](https://github.com/celestebyte)!

## 5.0.6 - 2024-06-29

- [Re: #98](https://github.com/supabase-community/gotrue-csharp/pull/98) Introduces `VerifyTokenHash` to support the
  PKCE flow for email signup. Thanks [@alexbakker](https://github.com/alexbakker)!

## 5.0.5 - 2024-06-11

- Allow for scoped `SignOut`. Thanks [@AndrewKahr](https://github.com/AndrewKahr)!

## 5.0.4 - 2024-06-09

- Various minor SSO fixes. Thanks [@Rycko1](https://github.com/Rycko1)!

## 5.0.3 - 2024-05-31

- Implement `SignInWithSSO`. Huge thank you to [@Rycko1](https://github.com/Rycko1)!

## 5.0.2 - 2024-05-20

- Add missing properties (`ProviderRefreshToken` and `ProviderToken`) to `Session` object to reflect current state
  of `auth-js`

## 5.0.1 - 2024-05-16

- Re: [#96](https://github.com/supabase-community/postgrest-csharp/pull/96) Set `ConfigureAwait(false)` the response to
  prevent deadlocking applications. Thanks [@pur3extreme](https://github.com/pur3extreme)!

## 5.0.0 - 2024-04-21

- Re: [#135](supabase-community/supabase-csharp#135) Update nuget package name `gotrue-csharp` to `Supabase.Gotrue`
- Update dependencies

## 4.3.1 - 2024-04-05

- Re: [#89](https://github.com/supabase-community/gotrue-csharp/issues/89), Only add `access_token` to request body when
  it is explicitly declared.

## 4.3.0 - 2024-04-04

- [MINOR] Re: [#89](https://github.com/supabase-community/gotrue-csharp/issues/89) Update signature
  for `SignInWithIdToken` which adds an optional `accessToken` parameter, update doc comments, and call `DestroySession`
  in method
- Re: [#88](https://github.com/supabase-community/gotrue-csharp/issues/88), Add `IsAnonymous` property to `User`
- Re: [#90](https://github.com/supabase-community/gotrue-csharp/issues/90) Implement `LinkIdentity` and `UnlinkIdentity`

## 4.2.7 - 2024-04-02

- [#88](https://github.com/supabase-community/gotrue-csharp/issues/88) Implement `signInAnonymously` from the JS client
- Include additional 3rd party providers in constants.

## 4.2.6 - 2023-12-30

- [#83](https://github.com/supabase-community/gotrue-csharp/pull/83) Replaces JWTDecoder package with
  System.IdentityModel.Tokens.Jwt. Thanks [@FantasyTeddy](https://github.com/FantasyTeddy)!

## 4.2.5 - 2023-12-15

- [#82](https://github.com/supabase-community/gotrue-csharp/issues/82) - Implements #82 - Creates a `GenerateLink`
  method on the `AdminClient` that supports `signup`, `invite`, `magiclink`, `recovery`, `email_change_new`
  and `email_change_current`

## 4.2.4 - 2023-12-1

- [#81](https://github.com/supabase-community/gotrue-csharp/issues/81) - Adds `InviteUserByEmailOptions` as a parameter
  to the Gotrue Admin Client

## 4.2.3 - 2023-10-11

- [#80](https://github.com/supabase-community/gotrue-csharp/pull/80) Fixes `Session.Expires()` not being calculated
  correctly. Thanks [@dayjay](https://github.com/Dayjay)!

## 4.2.2 - 2023-10-01

- [#78](https://github.com/supabase-community/gotrue-csharp/issues/78) - Implements PKCE flow support
  for `ResetPasswordForEmail`.

## 4.2.1 - 2023-08-19

- [#74](https://github.com/supabase-community/gotrue-csharp/pull/74) - Fixes bug where token refresh interval was not
  honored by client. Thanks [@slater1](https://github.com/slater1)!

## 4.2.0 - 2023-08-13

- **Minor Breaking changes:** [#72](https://github.com/supabase-community/gotrue-csharp/pull/72) - Fixes
  Calling `SetAuth` does not actually set Authorization Headers for subsequent requests by implementing `SetSession`
    - Removes `RefreshToken(string refreshToken)` and `SetAuth(string accessToken` in favor
      of `SetSession(string accessToken, string refreshToken)`
    - Makes `RefreshAccessToken` require `accessToken` and `refreshToken` as parameters - overrides the authorization
      headers to use the supplied token
    - Migrates project internal times to use `DateTime.UtcNow` over `DateTime.Now`.

## 4.1.1 - 2023-06-29

- [#68](https://github.com/supabase-community/gotrue-csharp/pull/68) Changes Network Status to use the interface instead
  of client

## 4.1.0 - 2023-06-25

- **Minor** [#66](https://github.com/supabase-community/gotrue-csharp/pull/66) - Separates out Admin JWT functionality
  into a
  separate `AdminClient`
- [#67](https://github.com/supabase-community/gotrue-csharp/pull/67) - Adds shutdown method which terminates the
  background refresh threads.
- Movement of much of the documentation for methods out of their classes and into their interfaces.
- Language features locked to C#9

Thanks to [@wiverson](https://github.com/wiverson) for this release!

## 4.0.5 - 2023-06-17

- [#63](https://github.com/supabase-community/gotrue-csharp/pull/63) - Refresh Thread bug fixes, adds offline support
  for stateful end user client. Huge thank you to [@wiverson](https://github.com/wiverson) for this work!

## 4.0.4 - 2023-06-10

- Uses new `Supabase.Core` assembly name.

## 4.0.3 - 2023-06-10

- Update assembly to `Supabase.Gotrue`

## 4.0.2 - 2023-05-15

- [#58](https://github.com/supabase-community/gotrue-csharp/issues/58) - Add support for the `reauthentication` endpoint
  which allows for secure password changes.

## 4.0.1 - 2023-05-11

- [#60](https://github.com/supabase-community/gotrue-csharp/pull/60) - Add interfaces, bug fixes, additional error
  reason detection. Thanks [@wiverson](https://github.com/wiverson)!

## 4.0.0

### [#57](https://github.com/supabase-community/gotrue-csharp/pull/57) Refactor exceptions, code cleanup, and move to delegate auth state changes

Huge thank you to [@wiverson](https://github.com/wiverson) for his help on this refactor and release!

- Changes
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
- New feature:
    - Added a `Settings` request to the stateless API only - you can now query the server instance to
      determine if it's got the settings you need. This might allow for things like a visual
      component in a tool to verify the GoTrue settings are working correctly, or tests that run differently
      depending on the server configuration.
- Implementation notes:
    - Test cases have been added to help ensure reliability of auth state change notifications
      and persistence.
    - Persistence is now managed via the same notifications as auth state change

## 3.1.2 - 2023-05-02

- [#49](https://github.com/supabase-community/gotrue-csharp/issues/49) Implements `SignInWithOtp` for email and phone

## 3.1.1 - 2023-04-27

- Implements `SignInWithIdToken` for Apple/Google signing from LW7. A HUGE thank you
  to [@wiverson](https://github.com/wiverson)!

## 3.1.0 - 2023-04-08

- [Minor] Implements PKCE auth flow. SignIn using a provider now returns an instance of `ProviderAuthState` rather than
  a `string`.

## 3.0.6 - 2023-03-29

- Supports adding `SignInOptions` (i.e. `RedirectTo`) on `OAuth Provider` SignIn requests.

## 3.0.5 - 2023-02-28

- Fixes [#44](https://github.com/supabase-community/gotrue-csharp/issues/44) - refresh timer should automatically
  reattempt (interval of 5s) for HTTP exceptions - gracefully exits on invalid refresh and triggers
  an `AuthState.Changed` event

## 3.0.4 - 2023-01-18

- Makes `Session.CreatedAt` a publicly settable property, which should fix incorrect dates on retrieved `Session`s.

## 3.0.3 - 2022-11-12

- [Hotfix] Fixed `GetHeaders` not being passed to the `API` instance

## 3.0.2 - 2022-11-12

- Use `supabase-core` and implement `IGettableHeaders` on `IGotrueAPI` and `IGotrueClient`
- `Api` no longer requires `headers` as a parameter.

## 3.0.1 - 2022-11-11

- `ClientOptions` interface updated to support a generic `TSession` to match the `IGotrueClient` interface.

## 3.0.0 - 2022-11-4

- [#40](https://github.com/supabase-community/gotrue-csharp/pull/40) Adjust to Dependency Injection Structure (
  Thanks [@HunteRoi](https://github.com/HunteRoi))
- [#34](https://github.com/supabase-community/supabase-csharp/issues/34) Enable nullability in project.

Migration from 2.x.x to 3.x.x:

- `Client` is no longer a Singleton - it should be initialized using its standard constructor.
- `StatelessClient` is no longer `Static` - it should be initialized using a standard constructor.
- Setting/Retrieving state on init has been disabled by default, you will need to call `client.RetrieveSessionAsync()`
  to retrieve state from your `SessionRetriever` function.

## 2.4.7 - 2022-10-31

- [#41](https://github.com/supabase-community/gotrue-csharp/issues/41) Add support
  for `VerifyOTP(string email, string token)`

## 2.4.6 - 2022-10-27

- [#39](https://github.com/supabase-community/gotrue-csharp/pull/39) Added GetUser method that supports a JWT. (
  tahnks [@AlexMeesters](https://github.com/AlexMeesters)!)

## 2.4.5 - 2022-05-24

- [#37](https://github.com/supabase-community/gotrue-csharp/issues/37) Adds a `SetAuth` method to allow setting an
  arbitrary JWT token.

## 2.4.4 - 2022-05-11

- [#33](https://github.com/supabase-community/gotrue-csharp/pull/32) Refresh timer should be cancelled if the user logs
  out, CurrentSession object may be null in RefreshToken

## 2.4.3 - 2022-04-27

- [#32](https://github.com/supabase-community/gotrue-csharp/pull/32) RefreshToken() should take an optional refresh
  token from the caller (Thanks [@RedChops](https://github.com/RedChops))

## 2.4.2 - 2022-04-23

- [#30](https://github.com/supabase-community/gotrue-csharp/pull/30) Update usage of `redirectTo` to reflect gotrue-js
  usage and adapt `GetSessionFromUrl` to gotrue's return format. (Thanks [@RedChops](https://github.com/RedChops))

## 2.4.1 - 2022-04-13

- Changed `UpdateUserById` to require the more specific `AdminUserAttributes` instead of `UserAttributes` (
  Thanks [@AydinE](https://github.com/AydinE))

## 2.4.0 - 2022-03-28

- [Minor API Change] - Some `User` Model Attributes will now hydrate as `null` instead of as the object `defaults` (
  i.e. `ConfirmedAt`)

## 2.3.6 - 2022-02-27

- Added providers for `LinkedIn` and `Notion`

## 2.3.5 - 2022-01-19

- [#23](https://github.com/supabase-community/gotrue-csharp/pull/23) Added `redirect_url` option for MagicLink sign in (
  Thanks [@MisterJimson](https://github.com/MisterJimson))

## 2.3.4 - 2022-01-07

- [#21](https://github.com/supabase-community/gotrue-csharp/pull/21) Added SignOut method to Stateless Client (
  Thanks [@fplaras](https://github.com/fplaras))

## 2.3.3 - 2021-12-29

- Minor: `SignUp` will return a `Session` with a _populated `User` object_ on an unconfirmed signup.
    - Fixes [#19](https://github.com/supabase-community/gotrue-csharp/issues/19)
    - Developers who were using a `null` check on `Session.User` will need to adjust accordingly.

## 2.3.2 - 2021-12-25

- Minor: `SignUp` signature now uses a class `SignUpOptions` to include `Data` and `RedirectTo` options. (
  Ref: [supabase-community/supabase-csharp#16](https://github.com/supabase-community/supabase-csharp/issues/16))
- Fix [#17](https://github.com/supabase-community/gotrue-csharp/issues/17)
  and [#18](https://github.com/supabase-community/gotrue-csharp/issues/18)

## 2.3.1 - 2021-12-24

- Minor: `CreateUser` signature exchanges `object userdata` with `AdminUserAttributes attributes`.
- [#16](https://github.com/supabase-community/gotrue-csharp/issues/16) Conforms `CreateUser` to
  the `AdminUserAttributes` request format.

## 2.3.0 - 2021-12-23

- [#15](https://github.com/supabase-community/gotrue-csharp/issues/15) Added optional `metadata` parameter for
  user `SignUp` functions.
- Introduces a change into `User.AppMetadata` and `User.UserMetadata` where types are now `Dictionary<string,object>`
  rather than just `object`.

## 2.2.4 - 2021-12-4

- [#14](https://github.com/supabase-community/gotrue-csharp/pull/14) Implemented `ListUsers` (paginate, sort,
  filter), `GetUserById`, `CreateUser`, `UpdateById` (
  Thanks [@TheOnlyBeardedBeast](https://github.com/TheOnlyBeardedBeast])!)

## 2.2.3 - 2021-12-2

- [#11](https://github.com/supabase-community/gotrue-csharp/pull/11) Add reset password capability (
  Thanks [@phxtho](https://github.com/phxtho)!)

## 2.2.2 - 2021-11-29

- [#12](https://github.com/supabase-community/supabase-csharp/issues/12) Add a `AuthState.TokenRefreshed` trigger on
  Token Refresh (along with test).

## 2.2.1 - 2021-11-24

- [#7](https://github.com/supabase-community/supabase-csharp/issues/7) Add a `StatelessClient` static class that enables
  API interactions through specifying `StatelessClientOptions`
- Added tests for `StatelessClient`
- Attempting to sign up a User that already exists throws a `BadRequestException` on the latest pull
  of `supabase/gotrue`so the appropriate tests have been updated.
- Internally, exceptions were moved to a `ExceptionHandler` class to be shared between `Client` and `StatelessClient`

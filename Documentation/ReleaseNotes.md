
# Release Notes & Breaking Changes

The Supabase C# library uses [semantic versioning](https://semver.org/). This is a pretty standard
versioning scheme - if you see a X.Y.Z version number, expect major breaks for a X version bump,
minor changes for a Y, and bug fixes for Z.

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

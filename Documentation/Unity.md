# Unity Support

The Client works with Unity. You can find an example of a session persistence
implementation for Unity at this [gist](https://gist.github.com/wiverson/fbb07498743dff19b72c9c58599931e9).

## Key Project Setup Details

UniTask is included in the project to help with async/await support. You don't want your game UI to lock up waiting for
network requests, so you'll have to get into the weeds on async/await.

Install the Unity-specific version of the NewtonSoft JSON libraries. This version is specifically designed to work with
Unity.

[Managed stripping is set to off/minimal](https://docs.unity3d.com/Manual/ManagedCodeStripping.html). This is mainly to
avoid issues around code stripping removing constructors for JSON-related operations and reflection. Some users have
reported setting custom ProGuard configurations also works.

Depending on your project, you may want to add Proguard rules to your project.
See [more info here](https://github.com/supabase-community/supabase-csharp/issues/87).

## Update and Dirty Flags

Often you may want to perform the following steps:

- User clicks on some UI element, which kicks off an async request
- The async request succeeds or fails
- Now you want to update the UI based on those results.

Unfortunately, Unity does not allow for async methods to be called by the UI builder. `Update`, however, can be declared
as async. So, to solve this we have to perform the following steps:

- When the user clicks on a UI element, set a flag/data indicating that this has happened.
- In the Update loop, check to see if this data flag has been set. If so, call the async method. Send the resulting
  information back to the calling class, either by setting the data to a field/property or via a callback.
- In the Update loop, check to see if the result data is set. If so, update the UI as appropriate.

## Notifications and Debug Info

You'll want to have some mechanism for sending notifications to the user when events occur. For example, when the
application boots up you'll likely want to refresh the user's session. That's an async request, so you'll want to have a
mechanism for posting the notification back to both the user and/or the application itself.

## Session Persistence

Note that for a variety of reasons you should only use local device preferences (e.g. screen resolution) with
PlayerPrefs. PlayerPrefs has a number of limitations, including only supporting access via the user thread.

With Supabase, you want the option to store the user session data to the filesystem - for example, if the background
thread refreshes the user session - without affecting the UI. Fortunately, we can use the standard .NET APIs for file
system access. This also has the side-effect of reducing the surface area of the Unity Engine for automated testing.

A session is a JWT and is roughly analogous to a cookie. If you am want to increase the security of the session storage
see suggestions under Save Password below.

## Network Status and Caching Data

You'll want to take the Supabase client offline when the user doesn't have a network connection to avoid accidentally
signing the user out. In addition you may want to limit the operations a user can perform when the network goes
online/offline.

## Unit Testing

Testing your Supabase integration is much, much easier if you can develop test cases that run in Unity and/or NUnit.
Unfortunately, as of this writing async test cases seem to only work with prerelease versions of the Unity Test
Framework.

You'll want to install version X via the Package Manager. Select Add By Name... and use this version. You can now
declare individual test cases with async declarations and they will work. There is a gotcha however - as of this writing
the Setup and Teardown methods cannot be declared as async and will fail.

You are encouraged to voice your support for a full 2.0 release of the Unity Test framework with full async support on
the forum.

# Implementing Save Password

Implementing save password functionality can be a nice way to streamline the user experience. However, this can present
a security risk if not implemented correctly. This is complicated by the lack of portable system level secure storage.

At a minimum, you s should look at a strategy that includes:

- Encrypt the user password on disk using a well known two way encryption algorithm.
- Use a randomly generated key for the encryption. Include an app-specific salt in the key.
- Store the randomly generated key and the encrypted password in different locations (eg Player Prefs and the
  application data directory).

In this scenario, a hostile actor would have to have access to the key, the salt, and the stored encrypted password.
This level of access probably means the device is completely compromised (eg on the level of a key logger or network
root certificate), which is usually out of scope for most applications.

# Complex Local Cache

If you would like to add more comprehensive support for local SQL storage of cached data, check out SQLite. There are a
variety of different options for setting up SQLite on Unity depending on your target platforms.

Implementing a local sync storage solution is outside the scope of this document. You may want to post ideas, questions,
and strategies to the forum.

# Unity Setup Step By Step

1. Install the NuGet CLI. Unity doesn't natively support NuGet, so you'll need to install the CLI and then install the
   package manually.

On macOS, the easiest way to do this is with [Homebrew](https://brew.sh/).

```
brew install nuget
```

On Windows, you can install the CLI with [Chocolatey](https://chocolatey.org/).

```
choco install nuget.commandline
```

2. Install the Supabase C# library using NuGet. On macOS this command looks like:

```
nuget install supabase-csharp -OutputDirectory ./Assets/Supabase -Framework netstandard2.0
```

You can add a version flag if you want to grab a specific version (e.g. `-Version 0.13.1`).

3. Delete conflicting/unneeded libraries.

Here are the core Supabase libraries:

- supabase-storage-csharp.1.4.0
- supabase-csharp.0.13.1
- supabase-core.0.0.3
- realtime-csharp.6.0.4
- postgrest-csharp.3.2.5
- gotrue-csharp.4.2.1
- functions-csharp.1.3.1

Here are the only required supporting libraries as of this writing:

- MimeMapping.2.0.0
- System.Reactive.5.0.0
- System.Threading.4.3.0
- System.Threading.Channels.5.0.0
- System.Threading.Tasks.4.3.0
- System.Threading.Tasks.Extensions.4.5.4
- Websocket.Client.4.6.1
- JWTDecoder.0.9.2

The rest of the libraries (mostly various System libraries) are already included in Unity
or are otherwise not required.

4. Install the Unity-specific version of the NewtonSoft JSON libraries. This version is specifically designed to work
   with Unity.

Open the Package Manager in Unity and press the + (plus) button in the upper-left corner of the window.

Choose the Add package by name option and enter `com.unity.nuget.newtonsoft-json`. Press
enter. You should see the `Newtonsoft Json` package appear (v3.2.1 as of this writing). Click on the package and then
click the Download/Install buttons as usual.

5. Install UniTask. UniTask is a library that provides async/await support for Unity. You can install it via the
   Package Manager. Open the Package Manager in Unity and press the + (plus) button in the upper-left corner of the
   window.

Choose the Add package by git URL... option and
enter `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`.
Press enter. You should see the UniTask package (v2.3.3 as o this writing). Download/install as usual.

6. (Optional) Install a preview version of the Unity test framework that supports running async tests.

This time, open up the Package Manager and select Add Package by name... Enter `com.unity.test-framework` for the
name of the package and `2.0.1-exp.2` for the version. This version was released on November 14, 2022. Unity may
prompt you to update to the 2.0.1-pre.18 version, but unfortunately this version was released on January 24, 2022
and is a downgrade.

7. (Optional) Install the native Sign in with Apple package.

This is only necessary if you plan to support native Sign in with Apple. You can find instructions for installation
[here](https://github.com/lupidan/apple-signin-unity).

8. You should be able to start working with Supabase!

For next steps, check out the [Desktop Clients](DesktopClients.md) documentation to see
an example of setting up a Supabase manager.

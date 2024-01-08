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

[**CHANGELOG is available in the repository root.**](https://github.com/supabase-community/supabase-csharp/blob/master/CHANGELOG.md)

## Features

- [x] Integration with [Supabase.Realtime](https://github.com/supabase-community/realtime-csharp)
  - Realtime listeners for database changes
- [x] Integration with [Postgrest](https://github.com/supabase-community/postgrest-csharp)
  - Access your database using a REST API generated from your schema & database functions
- [x] Integration with [Gotrue](https://github.com/supabase-community/gotrue-csharp)
  - User authentication, including OAuth, email/password, and native sign-in
- [x] Integration with [Supabase Storage](https://github.com/supabase-community/storage-csharp)
  - Store files in S3 with additional managed metadata 
- [x] Integration with [Supabase Edge Functions](https://github.com/supabase-community/functions-csharp)
  -  Run serverless functions on the edge
- [x] [Nuget Release](https://www.nuget.org/packages/supabase-csharp)

## Quickstart

1. To get started, create a new project in the [Supabase Admin Panel](https://app.supabase.io).
2. Grab your Supabase URL and Supabase Public Key from the Admin Panel (Settings -> API Keys).
3. Initialize the client!

_Reminder: `supabase-csharp` has some APIs that require the `service_key` rather than the `public_key` (for instance: the administration of users, bypassing database roles, etc.). If you are using the `service_key` **be sure it is not exposed client side.** Additionally, if you need to use both a service account and a public/user account, please do so using a separate client instance for each._

## Documentation

- [Getting Started](https://github.com/supabase-community/supabase-csharp/wiki#getting-started)
- [Unity](https://github.com/supabase-community/supabase-csharp/wiki/Unity)
- [Desktop/Mobile Clients (e.g. Xamarin, MAUI, etc.)](https://github.com/supabase-community/supabase-csharp/wiki/Desktop-Clients)
- [Server-Side Applications](https://github.com/supabase-community/supabase-csharp/wiki/Server-Side-Applications)
- [Release Notes/Breaking Changes](https://github.com/supabase-community/supabase-csharp/wiki/Release-Notes)
- [Using the Client](https://github.com/supabase-community/supabase-csharp/wiki#using-the-client)
- [Examples](https://github.com/supabase-community/supabase-csharp/wiki/Examples)

### Specific Features

- [Offline Support](https://github.com/supabase-community/supabase-csharp/wiki/Authorization-with-Gotrue#offline-support)
- [Refresh Token Thread](https://github.com/supabase-community/supabase-csharp/wiki/Authorization-with-Gotrue#updated-refresh-token-handling)
- [Native Sign in with Apple]([Documentation/NativeSignInWithApple.md](https://github.com/supabase-community/supabase-csharp/wiki/Authorization-with-Gotrue#native-sign-in-with-apple))

### Troubleshooting

- [Troubleshooting](https://github.com/supabase-community/supabase-csharp/wiki/Troubleshooting)
- [Discussion Forum](https://github.com/supabase-community/supabase-csharp/discussions)

## Package made possible through the efforts of:

<a href="https://github.com/supabase-community/supabase-csharp/graphs/contributors">
  <img src="https://contrib-generator.fly.dev/repo/generate?repo=supabase-community/supabase-csharp,supabase-community/postgrest-csharp,supabase-community/realtime-csharp,supabase-community/gotrue-csharp&size=64&strokeWidth=4&strokeColor=3ecf8e&padding=12"/>
</a>

Join the ranks! See a problem? Help fix it!

## Contributing

We are more than happy to have contributions! Please submit a PR.

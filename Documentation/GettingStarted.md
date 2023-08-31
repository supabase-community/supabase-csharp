# Getting Started

The Supabase C# library is a wrapper around the various REST APIs provided by Supabase and
the various server components (e.g. GoTrue, Realtime, etc.).

## Video Options

If you prefer video format: [@Milan Jovanović](https://www.youtube.com/@MilanJovanovicTech) has created a [video crash course to get started](https://www.youtube.com/watch?v=uviVTDtYeeE)!

## Getting Oriented

At the most basic level, Supabase provides services based on the Postgres database and
the supporting ecosystem. You can use the online, hosted version of Supabase, run it locally
via CLI and Docker images, or some other combination (e.g. hosted yourself on another cloud service).

One option for .NET developers, of course, is to just treat it as any other RDBMS package. You can
create a project, grab the .NET connection string, and use it as you would any other database.

However, Supabase also provides a number of other services that are useful for building applications
and services. These include:

- Authentication ([GoTrue](https://github.com/supabase/gotrue))
- PostgREST ([REST API](https://postgrest.org/en/stable/))
- [Realtime](https://github.com/supabase/realtime)
- [Storage](https://github.com/supabase/storage-api)
- [Edge Functions](https://github.com/supabase/edge-runtime)

Authentication is provided by GoTrue, which is a JWT-based authentication service. It provides
a number of features, including email/password authentication, OAuth, password resets, email
verification, and more. In addition, you can use it to handle the native Sign in With Apple and
Sign in With Google authentication systems.

PostgREST is a REST API that is automatically generated from your database schema. It provides
a number of features, including filtering, sorting, and more.

Realtime is a service that provides realtime updates to your database. It is based on Postgres
LISTEN/NOTIFY and WebSockets.

Storage is a service that provides a simple interface for storing files. It is based on Postgres
and provides a number of features, including file versioning, metadata, and more.

Edge Functions is a service that provides a way to run serverless functions on the edge.

The Supabase C# library provides a wrapper around the various REST APIs provided by Supabase and
the various server components (e.g. GoTrue, Realtime, etc.). It also provides a number of
convenience methods and classes - for example, utilities to make the native Sign in with Apple
flow easier.

Care has been taken to mirror, as much as possible, the [Javascript Supabase API](https://github.com/supabase/supabase-js). As this is an unofficial client, there are times where this client lags behind the offical client. **If there are missing features, please open an issue or pull request!**

## Basic Concepts

There are two main ways to access your Supabase instance - either via an "untrusted" client
(e.g. Unity or some other mobile/desktop client) or a "trusted" client (e.g. a server-side application).

The untrusted clients have two key factors - first, you'll likely want to manage the user
state (e.g. login, logout, etc.) and second, you'll be using the anonymous/public API key to
access those services. The user is expected to use some kind of credentials (e.g. email/password,
OAuth, or a JWT from a native sign-in dialog, etc.) to access the services. The Supabase session
(a JWT issued by GoTrue) serves as the authentication token for the various services.

Tip: If you aren't familiar with JWTs, you can read more about them [here](https://jwt.io/introduction/).
You can also use this site to decode the JWTs issued by GoTrue, which you may find helpful
when learning about Supabase Auth. If you are a traditional server-side dev, you might find
it helpful to think of the JWT as a session cookie, except that it is cryptographically signed
(proving that it was "minted" by GoTrue). You can use standard JWT libraries to decode the
token, access the details, and verify the signature.

Trusted, server-side code is usually best managed as a stateless system, where each request
is managed independently. In this scenario, you will often want to use the library in conjunction
with the private API key.

**Remember - the public key is designed to be used in untrusted clients, while the private key
is designed to be used in trusted clients ONLY.**

## Next Steps

Given that the configuration is pretty different depending on the scenario, we'll cover each
of the scenarios separately.

- [Unity](Unity.md)
- [Desktop Clients](DesktopClients.md)
- [Server-Side Applications](ServerSideApplications.md)

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


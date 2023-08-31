# Supabase And .NET Server-Side Applications

At the most basic level, Supabase is a Postgres database. As such, you can create a project, grab the .NET connection
string,
and use it as you would any other database.

However, if you use the Supabase Hosted service, you'll also have access to a number of other services that are useful
for
building applications and services.

For many applications, you may want to start by looking at the standard Supabase JavaScript client
library. For many flows, you may just want to use that library in the client. For example, you might
want to rely on the JavaScript client for the bulk of the user authentication flow, and then pass along
the signed-in user JWT to your server-side application via cookie, and then use that JWT to perform
a few key C# REST operations.

You may want to think carefully about how you want to manage connectivity. For example, while you
_could_ route all of the Realtime traffic from a client side app to your .NET server, and then route
that back again to the client, it's probably better to just use the Realtime client directly from
the client-side app.

## Nomenclature Warning

The Supabase stateless library technically does store state in an instance - it stores the JWT token and the
refresh token.

In practice, this means that you should not reuse the same stateless client across multiple
requests. Instead, you should create a new client for each request. While this does make things a bit
more confusing (as well as put a tiny bit more load on the garbage collector), this is closer to the
way the base Supabase JS client initially worked. At some point in the future, we may add a true stateless
client that does not store any state.

## Installation

The [library is available via NuGet](https://www.nuget.org/packages/supabase-csharp). Depending on your
environment, you can install it via the command line:

```
dotnet add package supabase-csharp
```

Many IDEs also provide a way to install NuGet packages.

## Getting Started

You can use the base Supabase client to access the individual services, or you can just grab the
individual client that you need.

For example, here is a way to use the GoTrue stateless client directly. This fragment of code
can seen in more detail in
the [corresponding test case](https://github.com/supabase-community/gotrue-csharp/blob/master/GotrueTests/StatelessClientTests.cs).

```csharp
using static Supabase.Gotrue.StatelessClient;

IGotrueStatelessClient<User, Session> client = new StatelessClient();

var user = $"{RandomString(12)}@supabase.io";
var serviceRoleKey = GenerateServiceRoleToken();
var result = await _client.InviteUserByEmail(user, serviceRoleKey, Options);
```

The main thing to note is the serviceRoleKey, which is the key available
in the Supabase Admin UI labeled `service_role`. This key is an admin key -
it can be used to perform any operation on the service. As such, you should
treat it like a password and keep it safe.

## Next Steps

- Read the [Supabase documentation](https://supabase.com/docs), watch videos, etc.
- Browse the [API documentation](https://supabase-community.github.io/supabase-csharp/api/Supabase.html) for the
  Supabase C# client.
- Check out the Examples and Tests for each of the sub-project libraries.
- Check out the [Supabase C# discussion board](https://github.com/supabase-community/supabase-csharp/discussions)
- Have fun!

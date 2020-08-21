<p align="center">
<img width="300" src=".github/supabase-csharp.svg"/>
</p>

---

# supabase-csharp (**VERY MUCH A WIP**)

Integrate your [Supabase](https://supabase.io) projects with C#.

Includes C# features to make supabase function more like an ORM - specifically the ability to leverage **strongly typed models**.

API is heavily modeled after the [supabase-js repo](https://github.com/supabase/supabase-js) and [postgrest-js repo](https://github.com/supabase/postgrest-js).

## Status

- [X] Supabase.Auth
- [ ] **(In Progress)** Supabase.Realtime
- [ ] **(In Progress)** PostgREST
- [ ] **(In Progress)** Unit/Integration Testing
- [ ] **(Planned)** Nuget Release


## Auth API

Authentication via this client _is_ platform agnostic - because of this, session persistance is not included by default.

To enable persistance, you will have to specify handlers into the passed `Supabase.Auth.ClientOptions` that will store a session on a users's device (either browser storage or device storage).

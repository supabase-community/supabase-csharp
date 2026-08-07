# Supabase.Postgrest

[![Build and Test](https://github.com/supabase-community/postgrest-csharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/supabase-community/postgrest-csharp/acionts/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Supabase.Postgrest)](https://www.nuget.org/packages/Supabase.Postgrest/)

---

## [Notice]: v4.0.0 renames this package from `postgrest-csharp` to `Supabase.Postgrest`. Which includes changing the namespace from `Postgrest` to `Supabase.Postgrest`.

## Now supporting (many) LINQ expressions!

```c#
await client.Table<Movie>()
            .Select(x => new object[] { x.Id, x.Name, x.Tags, x.ReleaseDate })
            .Where(x => x.Tags.Contains("Action") || x.Tags.Contains("Adventure"))
            .Order(x => x.ReleaseDate, Ordering.Descending)
            .Get();

await client.Table<Movie>()
            .Set(x => x.WatchedAt, DateTime.Now)
            .Where(x => x.Id == "11111-22222-33333-44444")
            // Or .Filter(x => x.Id, Operator.Equals, "11111-22222-33333-44444")
            .Update();

```

---

Documentation can be found [here](https://supabase-community.github.io/postgrest-csharp/api/Supabase.Postgrest.html).

Postgrest-csharp is written primarily as a helper library
for [supabase/supabase-csharp](https://github.com/supabase/supabase-csharp), however, it should be easy enough to use
outside of the supabase ecosystem.

The bulk of this library is a translation and c-sharp-ification of
the [supabase/postgrest-js](https://github.com/supabase/postgrest-js) library.

## Getting Started

Postgrest-csharp is _heavily_ dependent on Models deriving from `BaseModel`. To interact with the API, one must have the
associated
model specified.

To use this library on the Supabase Hosted service but separately from the `supabase-csharp`, you'll need to specify
your url and public key like so:

```c#
var auth = new Supabase.Gotrue.Client(new ClientOptions<Session>
{
    Url = "https://PROJECT_ID.supabase.co/auth/v1",
    Headers = new Dictionary<string, string>
    {
        { "apikey", SUPABASE_PUBLIC_KEY },
        { "Authorization", $"Bearer {SUPABASE_USER_TOKEN}" }
    }
})
```

Leverage `Table`,`PrimaryKey`, and `Column` attributes to specify names of classes/properties that are different from
their C# Versions.

```c#
[Table("messages")]
public class Message : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; }

    [Column("channel_id")]
    public int ChannelId { get; set; }

    public override bool Equals(object obj)
    {
        return obj is Message message &&
                Id == message.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}
```

Utilizing the client is then just a matter of instantiating it and specifying the Model one is working with.

```c#
void Initialize()
{
    var client = new Client("http://localhost:3000");

    // Get All Messages
    var response = await client.Table<Message>().Get();
    List<Message> models = response.Models;

    // Insert
    var newMessage = new Message { UserName = "acupofjose", ChannelId = 1 };
    await client.Table<Message>().Insert();

    // Update
    var model = response.Models.First();
    model.UserName = "elrhomariyounes";
    await model.Update();

    // Delete
    await response.Models.Last().Delete();
}
```

## Foreign Keys, Join Tables, and Relationships

The Postgrest server does introspection on relationships between tables and supports returning query data from
tables with these included. **Foreign key constrains are required for postgrest to detect these relationships.**

This library implements the attribute, `Reference` to specify on a model when a relationship should be included in a
query.

- [One-to-one Relationships](https://postgrest.org/en/stable/api.html#one-to-one-relationships): One-to-one
  relationships are detected if there’s an unique constraint on a foreign key.
- [One-to-many Relationships](https://postgrest.org/en/stable/api.html#one-to-many-relationships): The inverse
  one-to-many relationship between two tables is detected based on the foreign key reference.
- [Many-to-many Relationships](https://postgrest.org/en/stable/api.html#many-to-many-relationships): Many-to-many
  relationships are detected based on the join table. The join table must contain foreign keys to other two tables and
  they must be part of its composite key.

Given the following schema:

![example schema](.github/postgrest-relationship-example.drawio.png)

We can define the following models:

```c#
[Table("movie")]
public class Movie : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Reference(typeof(Person))]
    public List<Person> Persons { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("person")]
public class Person : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Reference(typeof(Profile))]
    public Profile Profile { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("profile")]
public class Profile : BaseModel
{
    [PrimaryKey("person_id")]
    public int PersonId { get; set; }

    [Column("email")]
    public string Email { get; set; }
}
```

**Note that each related model should inherit `BaseModel` and specify its `Table` and `Column` attributes as usual.**

The `Reference` Attribute by default will include the referenced model in all GET queries on the table (this can be
disabled
in its constructor).

As such, a query on the `Movie` model (given the above) would return something like:

```js
[
    {
        id: 1,
        created_at: "2022-08-20T00:29:45.400188",
        name: "Top Gun: Maverick",
        person: [
            {
                id: 1,
                created_at: "2022-08-20T00:30:02.120528",
                first_name: "Tom",
                last_name: "Cruise",
                profile: {
                    person_id: 1,
                    email: "tom.cruise@supabase.io",
                    created_at: "2022-08-20T00:30:33.72443"
                }
            },
            {
                id: 3,
                created_at: "2022-08-20T00:30:33.72443",
                first_name: "Bob",
                last_name: "Saggett",
                profile: {
                    person_id: 3,
                    email: "bob.saggett@supabase.io",
                    created_at: "2022-08-20T00:30:33.72443"
                }
            }
        ]
    },
    // ...
]
```

### Circular References

Circular relations can be added between models, however, circular relations should only be parsed one level deep for
models. For example, given the
models [here](https://github.com/supabase-community/postgrest-csharp/blob/master/PostgrestTests/Models/LinkedModels.cs),
a raw response would look like the following (note that the `Person` object returns the root `Movie` and
the `Person->Profile` returns its root `Person` object).

If desired, this can be avoided by making specific join models that do not have the circular references.

```json
[
  {
    "id": "68722a22-6a6b-4410-a955-b4eb8ca7953f",
    "created_at": "0001-01-01T05:51:00",
    "name": "Supabase in Action",
    "person": [
      {
        "id": "6aa849d8-dd09-4932-bc6f-6fe3b585e87f",
        "first_name": "John",
        "last_name": "Doe",
        "created_at": "0001-01-01T05:51:00",
        "movie": [
          {
            "id": "68722a22-6a6b-4410-a955-b4eb8ca7953f",
            "name": "Supabase in Action",
            "created_at": "0001-01-01T05:51:00"
          }
        ],
        "profile": {
          "person_id": "6aa849d8-dd09-4932-bc6f-6fe3b585e87f",
          "email": "john.doe@email.com",
          "created_at": "0001-01-01T05:51:00",
          "person": {
            "id": "6aa849d8-dd09-4932-bc6f-6fe3b585e87f",
            "first_name": "John",
            "last_name": "Doe",
            "created_at": "0001-01-01T05:51:00"
          }
        }
      },
      {
        "id": "07abc67f-bf7d-4865-b2c0-76013dc2811f",
        "first_name": "Jane",
        "last_name": "Buck",
        "created_at": "0001-01-01T05:51:00",
        "movie": [
          {
            "id": "68722a22-6a6b-4410-a955-b4eb8ca7953f",
            "name": "Supabase in Action",
            "created_at": "0001-01-01T05:51:00"
          }
        ],
        "profile": {
          "person_id": "07abc67f-bf7d-4865-b2c0-76013dc2811f",
          "email": "jane.buck@email.com",
          "created_at": "0001-01-01T05:51:00",
          "person": {
            "id": "07abc67f-bf7d-4865-b2c0-76013dc2811f",
            "first_name": "Jane",
            "last_name": "Buck",
            "created_at": "0001-01-01T05:51:00"
          }
        }
      }
    ]
  }
]
```

### Top Level Filtering

**By default** relations expect to be used as top level filters on a query. If following the models above, this would
mean that a `Movie` with no `Person` relations on it would not return on a query **unless** the `Relation`
has `useInnerJoin` set to `false`:

The following model would return any movie, even if there are no `Person` models associated with it:

```c#
[Table("movie")]
public class Movie : BaseModel
{
    [PrimaryKey("id")] 
    public string Id { get; set; }

    [Column("name")] 
    public string? Name { get; set; }

    [Reference(typeof(Person), useInnerJoin: false)]
    public List<Person> People { get; set; } = new();
}
```

### Inserting Related Records

PostgREST _does not support nested inserts or upserts_ — a request writes to exactly one table. Because of this,
`Reference` properties on a model are **ignored** on insert, update, and upsert, regardless of the relationship type —
one-to-one, one-to-many, and many-to-many alike. Inserting a `Movie` with its `Persons` list populated will persist the
movie but write nothing else, without raising an error.

To create a relationship, write the foreign key where it lives in the database:

**One-to-one / many-to-one** — the foreign key is a column on the row being inserted. Expose it on the model (as
the `Profile` model above does with `person_id`) and set it directly:

```c#
// `profile.person_id` references `person.id` — setting the column creates the relationship.
await client.Table<Profile>().Insert(new Profile { PersonId = person.Id, Email = "tom.cruise@supabase.io" });
```

**One-to-many** — the foreign key is a column on each child row. Insert the parent first (by default, the response
contains the inserted record including database-generated values such as its primary key), then bulk-insert the
children with their foreign key column set to the parent's key. Given a `review` table referencing `movie`:

```c#
[Table("review")]
public class Review : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("movie_id")]
    public int MovieId { get; set; }

    [Column("content")]
    public string Content { get; set; }
}
```

```c#
// 1. Insert the parent and retrieve its database-generated key.
var response = await client.Table<Movie>().Insert(new Movie { Name = "Top Gun: Maverick" });
var movie = response.Model!;

// 2. Bulk-insert the children with their foreign key column set.
var reviews = contents
    .Select(content => new Review { MovieId = movie.Id, Content = content })
    .ToList();
await client.Table<Review>().Insert(reviews);
```

**Many-to-many** — the foreign keys live in a join table that has no counterpart in your domain model, so it must be
modeled and written explicitly:

```c#
[Table("movie_person")]
public class MoviePerson : BaseModel
{
    [Column("movie_id")]
    public int MovieId { get; set; }

    [Column("person_id")]
    public int PersonId { get; set; }
}
```

```c#
// 1. Insert the root record and retrieve its database-generated key.
var response = await client.Table<Movie>().Insert(new Movie { Name = "Top Gun: Maverick" });
var movie = response.Model!;

// 2. Insert all join rows in a single bulk request.
var moviePersons = persons
    .Select(person => new MoviePerson { MovieId = movie.Id, PersonId = person.Id })
    .ToList();
await client.Table<MoviePerson>().Insert(moviePersons);
```

Whenever related records are written in multiple requests, the requests are **not atomic** — if a later request fails,
the earlier records exist without their relationships. When atomicity matters, wrap the writes in a
[database function](https://supabase.com/docs/guides/database/functions) and call it through `Rpc`:

```c#
await client.Rpc("insert_movie_with_persons", new Dictionary<string, object>
{
    { "name", "Top Gun: Maverick" },
    { "person_ids", persons.Select(p => p.Id).ToList() }
});
```

**Further Notes**:

- Postgrest _does not support nested inserts or upserts_. Relational keys on models will be ignored when attempting to
  insert or upsert on a root model (see [Inserting Related Records](#inserting-related-records)).
- The `Relation` attribute uses reflection to only select the attributes specified on the Class Model (i.e.
  the `Profile` model only declares properties for `person_id` and `email`, so only those columns will be requested in
  the query).

## Observability (OpenTelemetry)

The client emits traces and metrics through `System.Diagnostics`, so you can wire them into
OpenTelemetry (or any `ActivityListener`/`MeterListener`) without the client taking a dependency
on the OpenTelemetry packages. Emission is zero-cost while nothing is listening, so it is always
on and stays silent until you subscribe.

Register the client's `ActivitySource` and `Meter` by name. Use the `PostgrestDiagnostics.SourceName`
constant rather than hardcoding the string, so a typo becomes a compile error instead of a silent
no-op:

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Supabase.Postgrest;

// Requires the OpenTelemetry.Extensions.Hosting and an exporter package (e.g. OTLP) in your app.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(PostgrestDiagnostics.SourceName)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(PostgrestDiagnostics.SourceName)
        .AddOtlpExporter());
```

Once subscribed you get:

- A client span per request, named `{METHOD} {path}` and following OpenTelemetry HTTP conventions
  (method, status code, and a sanitized URL). The query string is **never** recorded — in Postgrest
  it carries the column filters and their values, which are potential PII. A `db.operation` tag
  (`select`, `insert`, `update`, `upsert`, `delete`, `count`, `rpc`) distinguishes the logical
  operation, since several map to the same HTTP verb.
- A `supabase.postgrest.http.request.duration` histogram (seconds), tagged with method, host, path,
  operation, and status code.

If you are not using the OpenTelemetry SDK, a raw listener works too:

```csharp
using System.Diagnostics;
using Supabase.Postgrest;

using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == PostgrestDiagnostics.SourceName,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => Console.WriteLine($"{activity.OperationName} {activity.Duration.TotalMilliseconds}ms {activity.Status}")
};
ActivitySource.AddActivityListener(listener);
```

This replaces the debug handler surface (`AddDebugHandler` and friends), which is now deprecated and
will be removed in a future major version.

## Status

- [x] Connects to PostgREST Server
- [x] Authentication
- [x] Basic Query Features
    - [x] CRUD
    - [x] Single
    - [x] Range (to & from)
    - [x] Limit
    - [x] Limit w/ Foreign Key
    - [x] Offset
    - [x] Offset w/ Foreign Key
- [x] Advanced Query Features
    - [x] Filters
    - [x] Ordering
- [ ] Custom Serializers
    - [ ] [Postgres Range](https://www.postgresql.org/docs/9.3/rangetypes.html)
        - [x] `int4range`, `int8range`
        - [ ] `numrange`
        - [ ] `tsrange`, `tstzrange`, `daterange`
- [x] Models
    - [x] `BaseModel` to derive from
    - [x] Coercion of data into Models
- [x] Unit Testing
- [x] Nuget Package and Release

## Package made possible through the efforts of:

| <img src="https://github.com/acupofjose.png" width="150" height="150"> | <img src="https://github.com/elrhomariyounes.png" width="150" height="150"> |
|:----------------------------------------------------------------------:|:---------------------------------------------------------------------------:|
|              [acupofjose](https://github.com/acupofjose)               |            [elrhomariyounes](https://github.com/elrhomariyounes)            |

## Contributing

We are more than happy to have contributions! Please submit a PR.

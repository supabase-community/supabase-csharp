<p align="center">
<img width="300" src=".github/supabase-csharp.png"/>
</p>

<p align="center">
  <img src="https://github.com/supabase/supabase-csharp/workflows/Build%20And%20Test/badge.svg"/>
  <a href="https://www.nuget.org/packages/supabase-csharp/">
    <img src="https://img.shields.io/nuget/vpre/supabase-csharp"/>
  </a>
</p>

## Stage: (Alpha) / Testing

---

Integrate your [Supabase](https://supabase.io) projects with C#.

Includes C# features to make supabase function more like an ORM - specifically the ability to leverage **strongly typed models**.

API is heavily modeled after the [supabase-js repo](https://github.com/supabase/supabase-js) and [postgrest-js repo](https://github.com/supabase/postgrest-js).

## Status

- [x] Integration with [Supabase.Realtime](https://github.com/supabase/realtime-csharp)
- [x] Integration with [Postgrest](https://github.com/supabase/postgrest-csharp)
- [x] Integration with [Gotrue](https://github.com/supabase/supabase-csharp)
- [ ] Unit/Integration Testing
- [ ] Nuget Release

## Getting Started

Care has been taken to make API interactions mirror - as much as possible - the Javascript API. However, there are some places
where Supabase-csharp deviates to make use of C# goodies that Javascript doesn't have.

Getting started is pretty easy!

Grab your API URL and Public Key from the Supabase admin panel.

```c#
public async void Main()
{
  // Make sure you set these (or similar)
  var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
  var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

  await Supabase.Client.InitializeAsync(url, key);
  // That's it - forreal. Crazy right?

  // The Supabase Instance can be accessed at any time using:
  //  Supabase.Client.Instance {.Realtime|.Auth|etc.}
  // For ease of readability we'll use this:
  var instance = Supabase.Client.Instance;

  // Access Postgrest using:
  var channels = await instance.From<Channel>().Get();

  // Access Auth using:
  await instance.Auth.SignIn(email, password);
  Debug.WriteLine(instance.Auth.CurrentUser.Id);

  // Interested in Realtime Events?
  var table = await instance.From<Channel>();
  table.On(ChannelEventType.Insert, Channel_Inserted);
  table.On(ChannelEventType.Delete, Channel_Deleted);
  table.On(ChannelEventType.Update, Channel_Updated);

  // Run a Remote Stored Procedure:
  await instance.Rpc("my_cool_procedure", params);
}
```

### Models:

Supabase-csharp is _heavily_ dependent on Models deriving from `SupabaseModel` (which derive from Postgrest-chsharp's `BaseModel`). To interact with the API, one must have the associated model specified.

Leverage `Table`,`PrimaryKey`, and `Column` attributes to specify names of classes/properties that are different from their C# Versions.

```c#
[Table("messages")]
public class Message : SupabaseModel
{
    // `ShouldInsert` Set to false so-as to honor DB generated key
    // If the primary key was set by the application, this could be omitted.
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; }

    [Column("channel_id")]
    public int ChannelId { get; set; }
}
```

## Package made possible through the efforts of:

Join the ranks! See a problem? Help fix it!

| <img src="https://github.com/acupofjose.png" width="150" height="150"> |
| :--------------------------------------------------------------------: |
|              [acupofjose](https://github.com/acupofjose)               |

## Contributing

We are more than happy to have contributions! Please submit a PR.

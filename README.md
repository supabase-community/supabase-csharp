<p align="center">
<img width="300" src=".github/supabase-csharp.png"/>
</p>
<p align="center">
  <img src="https://github.com/supabase/supabase-csharp/workflows/Build%20And%20Test/badge.svg"/>
  <a href="https://www.nuget.org/packages/supabase-csharp/">
    <img src="https://img.shields.io/nuget/vpre/supabase-csharp"/>
  </a>
</p>

---

## BREAKING CHANGES MOVING FROM v0.5.X to v0.6.X

- `Client` is no longer a singleton, singleton interactions (if desired) are left to the developer to implement.
- `Client` supports injection of dependent clients after initialization via property:
  - `Auth`
  - `Functions`
  - `Realtime`
  - `Postgrest`
  - `Storage`
- `SupabaseModel` contains no logic but remains for backwards compatibility. (Marked `Obsolete`)
- `ClientOptions.ShouldInitializeRealtime` was removed (no longer auto initialized)
- `ClientOptions` now references an `ISupabaseSessionHandler` which specifies expected functionality for session persistence on Gotrue (replaces `ClientOptions.SessionPersistor`, `ClientOptions.SessionRetriever`, and `ClientOptions.SessionDestroyer`).

In Short:
```c#
// What was:
await Supabase.Client.InitializeAsync(url, key, new Supabase.SupabaseOptions { AutoConnectRealtime = true, ShouldInitializeRealtime = true });
var supabase = Supabase.Client.Instance

// Becomes:
var supabase = new Supabase.Client(url, key, new Supabase.SupabaseOptions { AutoConnectRealtime = true });
await supabase.InitializeAsync();
```

---

Integrate your [Supabase](https://supabase.io) projects with C#.

Includes C# features to make supabase function more like an ORM - specifically the ability to leverage **strongly typed models**.

API is heavily modeled after the [supabase-js repo](https://github.com/supabase/supabase-js) and [postgrest-js repo](https://github.com/supabase/postgrest-js).

## Status

- [x] Integration with [Supabase.Realtime](https://github.com/supabase-community/realtime-csharp)
- [x] Integration with [Postgrest](https://github.com/supabase-community/postgrest-csharp)
- [x] Integration with [Gotrue](https://github.com/supabase-community/gotrue-csharp)
- [x] Integration with [Supabase Storage](https://github.com/supabase-community/storage-csharp)
- [x] Integration with [Supabase Edge Functions](https://github.com/supabase-community/functions-csharp)
- [x] Nuget Release

## Projects / Examples / Templates

- [Blazor WASM Template using Supabase](SupabaseExamples/BlazorWebAssemblySupabaseTemplate) [Live demo](https://blazorwasmsupabasetemplate.web.app/) by [@rhuanbarros](https://github.com/rhuanbarros) 

(Create a PR to list your work here!)

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

  var client = new Supabase.Client(url, key);
  await client.InitializeAsync();
  // That's it - forreal. Crazy right?

  // Access Postgrest using:
  var channels = await client.From<Channel>().Get();

  // Access Auth using:
  await client.Auth.SignIn(email, password);
  Debug.WriteLine(client.Auth.CurrentUser.Id);

  // Interested in Realtime Events?
  var table = await client.From<Channel>();
  table.On(ChannelEventType.Insert, Channel_Inserted);
  table.On(ChannelEventType.Delete, Channel_Deleted);
  table.On(ChannelEventType.Update, Channel_Updated);

  // Invoke an Edge Function
  var result = await client.Functions.Invoke("hello", new Dictionary<string, object> { 
      { "name", "Ronald" } 
  });

  // Run a Remote Stored Procedure:
  await client.Rpc("my_cool_procedure", params);

  // Interact with Supabase Storage
  await client.Storage.CreateBucket("testing")

  var bucket = client.Storage.From("testing");

  var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
  var imagePath = Path.Combine(basePath, "Assets", "supabase-csharp.png");

  await bucket.Upload(imagePath, "supabase-csharp.png");

  // If bucket is public, get url
  bucket.GetPublicUrl("supabase-csharp.png"));

  // If bucket is private, generate url
  await bucket.CreateSignedUrl("supabase-csharp.png", 3600));

  // Download it!
  await bucket.Download("supabase-csharp.png", Path.Combine(basePath, "testing-download.png"));
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

<a href="https://github.com/supabase-community/supabase-csharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=supabase-community/supabase-csharp" />
</a>

Made with [contrib.rocks](https://contrib.rocks/preview?repo=supabase-community%2Fsupabase-csharp).

## Contributing

We are more than happy to have contributions! Please submit a PR.

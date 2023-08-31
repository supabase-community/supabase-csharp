### Using the Client

As for actually using the client, each service is listed as a property on `Supabase.Client`. Some services have helpers to make interactions easier. Properties are provided for every client in the event that advanced customization of the client is needed.

1. `Supabase.Postgrest`
    - Is better accessed using `supabase.From<ModelName>()` as it provides a wrapper class with some helpful accessors (see below)
2. `Supabase.Realtime`
    - If used for listening to `postgres_changes` can be accessed using: `supabase.From<ModelName>().On(listenerType, (sender, response) => {})`
    - Otherwise, use `Supabase.Realtime.Channel("channel_name")` for `Broadcast` and `Presence` listeners.

```csharp
// Get the Auth Client
var auth = supabase.Auth;

// Get the Postgrest Client for a Model
var table = supabase.From<TModel>();

// Invoke an RPC Call
await supabase.Rpc("hello_world", null);

// Invoke a Supabase Function
await supabase.Functions.Invoke("custom_function");

// Get the Storage Client
var storageBucket = supabase.Storage.From("bucket_name");

// Use syntax for broadcast, presence, and postgres_changes
var realtime = supabase.Realtime.Channel("room_1");

// Alternatively, shortcut syntax for postgres_changes
await supabase.From<TModel>().On(ListenType.All, (sender, response) =>
{
    switch (response.Event)
    {
        case Constants.EventType.Insert:
            break;
        case Constants.EventType.Update:
            break;
        case Constants.EventType.Delete:
            break;
    }

    Debug.WriteLine($"[{response.Event}]:{response.Topic}:{response.Payload.Data}");
});
```

# More Tips

- Take time to review your options. Which can be found in `Supabase.SupabaseOptions`.
- Be aware that many of the supabase features require permissions for proper access from a client. This is **especially true** for `realtime`, `postgres`, and `storage`. If you are having problems getting the client to pull data, **verify that you have proper permissions for the logged in user.**
- Connection to `Supabase.Realtime` is, by default, not enabled automatically, this can be changed in options.
- When logging in using the `Supabase.Auth` (Gotrue) client, state is managed internally. The currently logged in user's token will be passed to all the Supabase features automatically (via header injection).
- Token refresh enabled by default and is handled by a timer on the Gotrue client.
- Client libraries [listed above](#status) have additional information in their readme files.

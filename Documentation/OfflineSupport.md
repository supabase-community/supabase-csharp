# Offline Support

The Supabase Auth client supports online/offline usage. The Client now has a simple boolean option "Online"
which can be set to to false. This can be combined with the NetworkStatus class to allow the client
to automatically go online & offline based on the device's network status.

To use this new NetworkStatus, add the following:

```csharp
// Create the client
var client = new Client(new ClientOptions { AllowUnconfirmedUserSessions = true });
// Create the network status monitor
var status = new NetworkStatus();
// Tell the network status monitor to update the client's online status
status.Client = client;
// Start the network status monitor
await status.StartAsync();
// rest of the usual client configuration
```

Only the stateful Client supports this feature, and only for the managed user sessions.
Admin JWT methods and the stateless client are not affected.

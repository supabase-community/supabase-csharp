# Changelog

## [7.2.0](https://github.com/supabase-community/realtime-csharp/compare/v7.1.0...v7.2.0) (2025-05-13)


### Bug Fixes

* 22 - `SerializerSettings` were not being passed to `PostgresChangesResponse` ([f244281](https://github.com/supabase-community/realtime-csharp/commit/f244281782ca433f1b89a3818a02f5ea3eaaa17f))
* 9 ([16292b0](https://github.com/supabase-community/realtime-csharp/commit/16292b099c9db1f8b0fa28aa138da7143d1e0978))
* Change websocket lib for Blazor WASM to use realtime ([ba861d8](https://github.com/supabase-community/realtime-csharp/commit/ba861d842c66dcfb78378b0454a478b11ab57262))
* Change websocket lib for Blazor WASM to use realtime ([1821fa4](https://github.com/supabase-community/realtime-csharp/commit/1821fa40ed579c0b281ef4f008c529fc3eb27ad1))
* implement filter on PostgresChangeHandler ([#55](https://github.com/supabase-community/realtime-csharp/issues/55)) ([a41e4f7](https://github.com/supabase-community/realtime-csharp/commit/a41e4f719e2f0f65faf92915777218d5634c24fc))


### Miscellaneous Chores

* release 7.2.0 ([09374f4](https://github.com/supabase-community/realtime-csharp/commit/09374f4b0c8e7bb681a350a5604b0ad5e50fd1a5))

## 7.1.0 - 2025-03-10
- Implement postgres change filters by @diegofesanto [#55](https://github.com/supabase-community/realtime-csharp/pull/55)
- Fix exception thrown for browser/client side [#55](https://github.com/supabase-community/realtime-csharp/pull/55)

## 7.0.2 - 2024-07-26

- Update Dependency: `Websocket.Client@5.1.2`
- Update Dependency: `Supabase.Postgrest@4.0.3`
- [Re:#167](https://github.com/supabase-community/supabase-csharp/issues/167) Adds support for specifying `GetHeaders`
  on the `RealtimeClient` which are included on the initial request to the server to establish websocket connection.

## 7.0.1 - 2024-05-22

- Re: [#47](https://github.com/supabase-community/realtime-csharp/issues/47) Return a Task from `Track` and `Untrack`
  methods

## 7.0.0 - 2024-04-21

- Merges [#45](https://github.com/supabase-community/realtime-csharp/pull/45) - Updating the `Websocket.Client@5.1.1`
- Re: [#135](https://github.com/supabase-community/supabase-csharp/issues/135) Update nuget package
  name `realtime-csharp` to `Supabase.Realtime`
- Updates Dependencies

## 6.0.4 - 2023-07-15

- Fixes [#29](https://github.com/supabase-community/realtime-csharp/issues/29) Where the Realtime client could
  disconnect from channels after a few hours and fail to reconnect by removing the case where the `IsSubscribe` flag is
  flipped when encountering a channel error.

## 6.0.3 - 2023-06-10

- Updates usage of `Supabase.Core` assembly

## 6.0.2 - 2023-06-10

- Updates assembly name to `Supabase.Realtime`

## 6.0.1 - 2023-05-22

- Updates publishing action for future packages, includes README and icon.

## 6.0.0 - 2023-05-22

- Merges [#28](https://github.com/supabase-community/realtime-csharp/pull/28)
  and [#30](https://github.com/supabase-community/realtime-csharp/pull/30)
- The realtime client now takes a "fail-fast" approach. On establishing an initial connection, client will throw
  a `RealtimeException` in `ConnectAsync()` if the socket server is unreachable. After an initial connection has been
  established, the **client will continue attempting reconnections indefinitely until disconnected.**
- [Major, New] C# `EventHandlers` have been changed to `delegates`. This should allow for cleaner event data access over
  the previous subclassed `EventArgs` setup. Events are scoped accordingly. For example, the `RealtimeSocket` error
  handlers will receive events regarding socket connectivity; whereas the `RealtimeChannel` error handlers will receive
  events according to `Channel` joining/leaving/etc. This is implemented with the following methods prefixed by (
  Add/Remove/Clear):
    - `RealtimeBroadcast.AddBroadcastEventHandler`
    - `RealtimePresence.AddPresenceEventHandler`
    - `RealtimeSocket.AddStateChangedHandler`
    - `RealtimeSocket.AddMessageReceivedHandler`
    - `RealtimeSocket.AddHeartbeatHandler`
    - `RealtimeSocket.AddErrorHandler`
    - `RealtimeClient.AddDebugHandler`
    - `RealtimeClient.AddStateChangedHandler`
    - `RealtimeChannel.AddPostgresChangeHandler`
    - `RealtimeChannel.AddMessageReceivedHandler`
    - `RealtimeChannel.AddErrorHandler`
    - `Push.AddMessageReceivedHandler`
- [Major, new] `ClientOptions.Logger` has been removed in favor of `Client.AddDebugHandler()` which allows for
  implementing custom logging solutions if desired.
    - A simple logger can be set up with the following:
  ```c#
  client.AddDebugHandler((sender, message, exception) => Debug.WriteLine(message));
  ```
- [Major] `Connect()` has been marked `Obsolete` in favor of `ConnectAsync()`
- Custom reconnection logic has been removed in favor of using the built-in logic from `Websocket.Client@4.6.1`.
- Exceptions that are handled within this library have been marked as `RealtimeException`s.
- The local, docker-composed test suite has been brought back (as opposed to remotely testing on live supabase servers)
  to test against.
- Comments have been added throughout the entire codebase and an `XML` file is now generated on build.

## 5.0.5 - 2023-04-27

- Re: [#27](https://github.com/supabase-community/realtime-csharp/issues/27) `PostgresChangesOptions` was not
  setting `listenType` in constructor. Thanks [@Kuffs2205](https://github.com/Kuffs2205)

## 5.0.4 - 2023-03-23

- Re: [#26](https://github.com/supabase-community/realtime-csharp/pull/26) - Fixes Connect() not returning callback
  result when the socket isn't null. Thanks [@BlueWaterCrystal](https://github.com/BlueWaterCrystal)!

## 5.0.3 - 2023-03-09

- Re: [#25](https://github.com/supabase-community/realtime-csharp/issues/25) - Support Channel being resubscribed after
  having been unsubscribed, fixes rejoin timer being erroneously called on channel `Unsubscribe`.
  Thanks [@Kuffs2205](https://github.com/Kuffs2205)!

## 5.0.2 - 2023-03-02

- Re: [#24](https://github.com/supabase-community/realtime-csharp/issues/24) - Fixes join failing until reconnect
  happened + adds access token push on channel join. Big thank you to [@Honeyhead](https://github.com/honeyhead) for the
  help debugging and identifying!

## 5.0.1 - 2023-02-06

- Re: [#22](https://github.com/supabase-community/realtime-csharp/issues/22) - `SerializerSettings` were not being
  passed to `PostgresChangesResponse` - Thanks [@Shenrak](https://github.com/Shenrak) for the help debugging!

## 5.0.0 - 2023-01-31

- Re: [#21](https://github.com/supabase-community/realtime-csharp/pull/21) Provide API for `presence`, `broadcast`
  and `postgres_changes`
    - [Major, New] `Channel.PostgresChanges` event will receive the wildcard `*` changes event, not `Channel.OnMessage`.
    - [Major] `Channel.OnInsert`, `Channel.OnUpdate`, and `Channel.OnDelete` now conform to the server's payload
      of `Response.Payload.**Data**`
    - [Major] `Channel.OnInsert`, `Channel.OnUpdate`, and `Channel.OnDelete` now return `PostgresChangesEventArgs`
    - [Minor] Rename `Channel` to `RealtimeChannel`
    - Supports better handling of disconnects in `RealtimeSocket` and adds a `Client.OnReconnect` event.
    - [Minor] Moves `ChannelOptions` to `Channel.ChannelOptions`
    - [Minor] Moves `ChannelStateChangedEventArgs` to `Channel.ChannelStateChangedEventArgs`
    - [Minor] Moves `Push` to `Channel.Push`
    - [Minor] Moves `Channel.ChannelState` to `Constants.ChannelState`
    - [Minor] Moves `SocketResponse`, `SocketRequest`, `SocketResponsePayload`, `SocketResponseEventArgs`,
      and `SocketStateChangedEventArgs` to `Socket` namespace.
    - [New] Adds `RealtimeBroadcast`
    - [New] Adds `RealtimePresence`
    - [Improvement] Better handling of disconnection/reconnection

## 4.0.1 - 2022-11-08

- Bugfixes on previous release.

## 4.0.0 - 2022-11-08

- Re: [#17](https://github.com/supabase-community/realtime-csharp/pull/17) Restructure Project to support Dependency
  Injection and Enable Nullity
    - `Client` is no longer a singleton class.
    - `Channel` has a new constructor that uses `ChannelOptions`
    - `Channel.Parameters` has been changed in favor of `Channel.Options`
    - `Channel` and `Push` are now directly dependent on having `Socket` and `SerializerSettings` passed in as opposed
      to referencing the `Singleton` instance.
    - All publicly facing classes (that offer functionality) now include an Interface.

## 3.0.1 - 2022-05-28

- Fixed deserialization of `DateTimes`

## 3.0.0 - 2022-02-18

- Exchange existing websocket client: [WebSocketSharp](https://github.com/sta/websocket-sharp)
  for [Marfusios/websocket-client](https://github.com/Marfusios/websocket-client) which adds support for Blazor WASM
  apps.
  Ref: [#14](https://github.com/supabase-community/realtime-csharp/pull/14)

## 2.0.8 - 2021-12-30

- [#12](https://github.com/supabase-community/realtime-csharp/issues/12): Implement Upstream Realtime RLS Error
  Broadcast Handling
- `SocketResponse` now exposes a method: `OldModel`, that hydrates the `OldRecord` property into a model.

## 2.0.7 - 2021-12-25

- [#11](https://github.com/supabase-community/realtime-csharp/issues/11) `user_token` Channel parameter is now set in
  the `SetAuth` call.

## 2.0.6 - 2021-11-29

- Bugfix introduced by 2.0.5, remove exposed `Client.Instance.subscriptions`

## 2.0.5 - 2021-11-29

- Fixed test for (`Client: Join channels of format: {database}:{schema}:{table}:{col}=eq.{val}`)
- Add support for WALRUS `AccessToken` Pushes on every heartbeat
  see [#12](https://github.com/supabase-community/supabase-csharp/issues/12)

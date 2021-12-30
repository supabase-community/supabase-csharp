# Changelog

## 0.3.0 - 2021-12-30

- Update dependency: postgrest-csharp@2.0.6
    - Add support for `NullValueHandling` to be specified on a `Column` Attribute and for it to be honored on Inserts and Updates. Defaults to: `NullValueHandling.Include`.
        - Implements [#38](https://github.com/supabase-community/postgrest-csharp/issues/38)
- Update dependency: realtime-csharp@2.0.8
    - Implement Upstream Realtime RLS Error Broadcast Handler
        - Implements [#12](https://github.com/supabase-community/realtime-csharp/issues/12)
    - `SocketResponse` now exposes a method: `OldModel`, that hydrates the `OldRecord` property into a model.

## 0.2.12 - 2012-12-29

- Update dependency: gotrue-csharp@2.3.3
    - `SignUp` will return a `Session` with a *populated `User` object* on an unconfirmed signup.
        - Fixes [#19](https://github.com/supabase-community/gotrue-csharp/issues/19)
        - Developers who were using a `null` check on `Session.User` will need to adjust accordingly.
- Update dependency: postgrest-csharp@2.0.5
    - Fix for [#37](https://github.com/supabase-community/postgrest-csharp/issues/37) - Return Type `minimal` would fail to resolve because of incorrect `Accept` headers. Added header and test to verify for future.
    - Fix for [#36](https://github.com/supabase-community/postgrest-csharp/issues/36) - Inserting/Upserting bulk records would fail while doing an unnecessary generic coercion.

## 0.2.11 - 2021-12-24

- Update dependency: gotrue-csharp@2.3.2 (changes CreateUser parameters to conform to `AdminUserAttributes`)
    - See [#15](https://github.com/supabase-community/supabase-csharp/issues/15)
    - See [#16](https://github.com/supabase-community/supabase-csharp/issues/16)
- Update dependency: realtime-csharp@2.0.7
    - See [#13](https://github.com/supabase-community/supabase-csharp/issues/13)

## 0.2.10 - 2021-12-23

- Update dependency: gotrue-csharp@2.3.0 (adds metadata support for user signup, see [#14](https://github.com/supabase/community/issues/14))

## 0.2.9 - 2021-12-9

- Separate Storage client from Supabase repo and into `storage-csharp`, `supabase-csharp` now references new repo.

## 0.2.8 - 2021-12-4

- Update gotrue-csharp to 2.2.4
    - Adds support for `ListUsers` (paginate, sort, filter), `GetUserById`, `CreateUser`, and `UpdateById`

## 0.2.7 - 2021-12-2

- Update gotrue-csharp to 2.2.3
    - Adds support for sending password resets to users.

## 0.2.6 - 2021-11-29

- Support for [#12](https://github.com/supabase-community/supabase-csharp/issues/12)
- Update realtime-csharp to 2.0.6
- Update gotrue-csharp to 2.2.2
- Add `StatelessClient` re:[#7](https://github.com/supabase-community/supabase-csharp/issues/7)

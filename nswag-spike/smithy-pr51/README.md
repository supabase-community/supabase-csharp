# Supabase Smithy Models

Canonical [Smithy IDL](https://smithy.io/) definitions for the Supabase HTTP APIs. These models are the shared source-of-truth for SDK codegen spikes — each SDK team runs their own generator toolchain against the same models.

## Structure

```
smithy/
  model/
    common.smithy        # Shared shapes (StringList, etc.)
    storage.smithy       # Supabase Storage API (buckets, objects, TUS resumable uploads)
    functions.smithy     # Supabase Edge Functions API (invoke: GET/POST/PUT/PATCH/DELETE)
    database.smithy      # Supabase Database API (PostgREST row + RPC operations)
  openapi/
    StorageService.openapi.json   # Generated OpenAPI 3.0 — committed for SDK consumers
    FunctionsService.openapi.json # Generated OpenAPI 3.0 — committed for SDK consumers
    DatabaseService.openapi.json  # Generated OpenAPI 3.0 — committed for SDK consumers
  smithy-build.json      # Smithy build config (Smithy CLI / Gradle)
  patch-openapi.py       # Post-generation patches (see Known Limitations)
  README.md
```

## Generating the OpenAPI artifacts

**Requirements:** [Smithy CLI](https://smithy.io/2.0/guides/smithy-cli/cli-installation.html) or Gradle with the Smithy Gradle plugin.

```bash
cd smithy
smithy build        # emits openapi/ into smithy/build/smithy/*/openapi/
python patch-openapi.py build/smithy/storage-openapi/openapi/StorageService.openapi.json
python patch-openapi.py build/smithy/functions-openapi/openapi/FunctionsService.openapi.json
python patch-openapi.py build/smithy/database-openapi/openapi/DatabaseService.openapi.json
```

The committed files in `openapi/` are the patched outputs — SDK teams can consume them directly without installing Smithy.

## Services modelled

### Storage (`model/storage.smithy`)

Covers the full Supabase Storage HTTP API:

| Group | Operations |
|-------|-----------|
| Buckets | `ListBuckets`, `GetBucket`, `CreateBucket`, `UpdateBucket`, `EmptyBucket`, `DeleteBucket` |
| Objects | `MoveObject`, `CopyObject`, `DeleteObjects`, `ListObjects`, `GetObjectInfo`, `HeadObject` |
| Signed URLs | `CreateSignedUrl`, `CreateSignedUrls`, `CreateSignedUploadUrl` |
| Direct upload | `UploadObject` (POST multipart), `UpdateObject` (PUT multipart) — OpenAPI-only; see Known Limitations |
| TUS resumable | `CreateTusUpload` (POST), `UploadChunk` (PATCH), `GetUploadOffset` (HEAD) |

### Functions (`model/functions.smithy`)

Models all five HTTP methods on `/functions/v1/{functionName}`:

`InvokeFunctionGet`, `InvokeFunctionPost`, `InvokeFunctionPut`, `InvokeFunctionPatch`, `InvokeFunctionDelete`

Smithy requires one operation per HTTP method — a dispatch switch in the client maps `FunctionInvokeOptions.method` to the right operation at runtime.

### Database (`model/database.smithy`)

Covers the PostgREST HTTP API (base URL: `/rest/v1`):

| Group | Operations |
|-------|-----------|
| Row CRUD | `SelectRows` (GET), `InsertRows` (POST), `UpdateRows` (PATCH), `UpsertRows` (PUT), `DeleteRows` (DELETE) |
| RPC | `CallRpcPost` (POST), `CallRpcGet` (GET) |

All row operations target `/{table}`. The model captures fixed query params (`select`, `order`, `limit`, `offset`, `on_conflict`, `columns`) and well-known request/response headers (`Prefer`, `Range`, `Range-Unit`, `Accept`, `Accept-Profile`, `Content-Profile`, `Content-Range`).

Request and response bodies are typed as `Blob` because the row shape is fully dynamic (depends on the table schema).

Horizontal filter parameters (`?column=op.value`, e.g. `?id=eq.5`) are expressed via an `@httpQueryParams StringMap` member (`filters`) on each read/write input — each map entry becomes a separate query parameter. A `FilterOperator` enum documents all supported operators so they are generated as typed constants in every SDK. RPC GET uses the same pattern (map named `args`) for function-specific query parameters.

## Known Limitations

These are gaps found during the Swift spike (see [SDK-1103](https://linear.app/supabase/issue/SDK-1103)) that require workarounds or are out of scope for codegen:

| # | Gap | Workaround |
|---|-----|-----------|
| 1 | `@streaming blob` emits `format: byte` (base64) in OpenAPI; generators need `format: binary` | `patch-openapi.py` rewrites the format after generation |
| 2 | No native `multipart/form-data` trait in Smithy | `patch-openapi.py` injects `UploadObject`/`UpdateObject` multipart operations directly into the OpenAPI JSON |
| 3 | Smithy requires a fixed HTTP method per operation; Functions supports any method at runtime | Model 5 separate operations; client dispatches at runtime |
| 4 | `GET` + `@httpPayload` is illegal in Smithy | Separate `InvokeFunctionGetInput` shape without a body |
| 5 | Realtime (WebSocket / event-emitter) is incompatible with REST codegen | Out of scope; Realtime stays hand-written in all SDKs |
| 6 | Write operations return 204 (no body) by default and 200 with a body for `return=representation` — Smithy requires a single fixed success code | Model uses 200 throughout; SDK clients must tolerate empty response bodies |

## Scope for SDK spikes

The models here cover **Storage**, **Functions**, and **Database (PostgREST)**. Each SDK spike (see Linear issues SDK-1103 through SDK-1109) must also verify **Auth**:

- **Auth** — sign-in/up, token refresh, OTP, OAuth redirects, session management

Auth model does not exist yet. It may need to be added here, or the teams may determine that it is unsuitable for codegen (OAuth redirects and cookie-based session management are difficult to express in Smithy).

For PostgREST the key open question for each SDK spike is whether the transport-layer codegen is useful. The `database.smithy` model covers the full HTTP surface: fixed params (`select`, `order`, `limit`, `offset`), filter params via `@httpQueryParams` + `FilterOperator` enum, and all relevant headers. The only part that stays hand-written is the query-builder API (`.eq()`, `.like()`, etc.) that constructs the filter map — that is by design, not a model gap.

## Generator toolchains by SDK

Each SDK team runs their own generator against the OpenAPI artifacts:

| SDK | Candidate toolchain |
|-----|-------------------|
| Swift | `swift-openapi-generator` (spike done — see [PR #1047](https://github.com/supabase/supabase-swift/pull/1047)) |
| JavaScript/TypeScript | TypeSpec → `@hey-api/openapi-ts` or `@typespec/http-client-js` |
| Python | TypeSpec → `openapi-python-client` or `@typespec/http-client-python` |
| Dart/Flutter | OpenAPI Generator `dart-dio` (no official TypeSpec/Smithy Dart emitter) |
| C# | Kiota or `@typespec/http-client-csharp` |
| Go | `oapi-codegen` or `ogen` |
| Kotlin | `smithy-kotlin` (KMP-compatible) or custom TypeSpec emitter |

## Reference

- RFC: [Auto-generating parts of the Supabase SDKs](https://linear.app/supabase/project/rfc-auto-generating-parts-of-the-supabase-sdks-581579f2a632)
- Swift spike PR: [supabase/supabase-swift#1047](https://github.com/supabase/supabase-swift/pull/1047)
- Linear spike issues: SDK-1103 (Swift) · SDK-1104 (JS) · SDK-1105 (Python) · SDK-1106 (Dart) · SDK-1107 (C#) · SDK-1108 (Go) · SDK-1109 (Kotlin)

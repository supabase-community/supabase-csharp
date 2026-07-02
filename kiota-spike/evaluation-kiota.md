# Kiota evaluation — answers to the spike brief (SDK-1107)

Tool: Kiota 1.32.4 (`kiota generate -l CSharp`), input = committed Smithy→OpenAPI from
`supabase/sdk#51` (same `*.ready.json` inputs as the NSwag spike). All three services generated
and compiled on netstandard2.1 (see `spike-kiota.csproj`).

Legend: ✅ works as-generated · 🟡 works with a model/patch or caveat · ❌ not achievable.

## What's produced

A **request-builder tree**, not a flat client: one builder class per path segment.

| Service | Files | Example call |
|---|---:|---|
| Storage | 51 | `client.Bucket.GetAsync()`, `client.Bucket["id"].DeleteAsync()`, `client.ObjectNamespace[...]...PostAsync(multipart)` |
| Functions | 5 | `client.Functions.V1["name"].PostAsync(stream)` |
| Database | 5 | `client.Table["name"].GetAsync(cfg)` |

- Models are separate files, **proper PascalCase** (`FileSizeLimit`, `AllowedMimeTypes`).
- Each operation is thin (~12 LOC): build `RequestInformation` → `RequestAdapter.SendAsync(...)`.
  **All real transport lives in the Kiota runtime**, not in generated code.
- Runtime dependency: **`Microsoft.Kiota.Bundle`** → 8 packages (`Kiota.Abstractions`,
  `Http.HttpClientLibrary`, `Serialization.Json/Form/Text/Multipart`, `Std.UriTemplate`).

## Question-by-question

### 1. Streaming uploads — ✅ (better than NSwag)
- **Functions/raw-binary body:** generated as `PostAsync(System.IO.Stream body, …)` — native stream,
  **no model patch needed** (Kiota maps `application/octet-stream` → `Stream` even with the
  `format:byte` payload that made NSwag JSON-serialize). 
- **Multipart (`UploadObject`/`UpdateObject`):** `PostAsync(MultipartBody body, …)` — Kiota's native
  `MultipartBody` (from `Serialization.Multipart`); the file part is a `Stream`.

### 2. Streaming responses — ✅ (better than NSwag)
Binary/octet responses generate as `Task<Stream>` via `RequestAdapter.SendPrimitiveAsync<Stream>`,
read through the runtime with response-headers-read semantics. **No byte→binary model patch
needed** (NSwag required one). Exposes `Stream`, not `IAsyncEnumerable<byte[]>` — brief accepts
either.

### 3. HttpClient injection — ✅
Client is constructed with an `IRequestAdapter`; `HttpClientRequestAdapter` accepts an externally
supplied `HttpClient` (DI / `IHttpClientFactory`).

### 4. Middleware / handlers — ✅ (with a caveat)
Kiota ships a middleware **handler pipeline** (retry, redirect, auth, telemetry) on the request
adapter — richer than NSwag's partial hooks. Caveat: the **request-builder tree resists the
`partial`-class seam** — you extend behavior via handlers/the adapter, not by owning half a partial.

### 5. Multipart uploads — ✅
Native `MultipartBody` with a streaming `Stream` file part (see Q1). The injected multipart schema's
required non-file parts carry through (same model wart as NSwag).

### 6. Auth flows — ❌ (not modelled)
Auth is absent from `supabase/sdk#51`; nothing to generate. Same conclusion as NSwag: HTTP ops
would be generatable once modelled, session/refresh/PKCE stay hand-written. Model gap to raise.

### 7. PostgREST query builder — 🟡 minimal
Same as NSwag: generates the row/RPC builders + models; the fluent `.eq()/.select()/.order()`
builder stays entirely hand-written (row bodies are dynamic). Kiota adds no query-builder help.

### 8. AOT / trimming — ✅ (Kiota's headline win)
Serialization is **explicit `IParsable`** (`GetFieldDeserializers()` / `Serialize(ISerializationWriter)`)
— **zero reflection**, 0 `JsonSerializer` calls in generated code. Trim/NativeAOT-safe by
construction; the Kiota runtime libs are trimmable. This is exactly where NSwag fails.

### 9. Unity compatibility — 🟡
netstandard2.1 + reflection-free serialization is IL2CPP-friendlier than NSwag. Not verified under
Unity here, but structurally the better of the two. The 8-package runtime is extra surface to ship.

### 10. Model gaps found
Same upstream gaps as the NSwag pass (Auth unmodelled; `{wildcardPath+}`; required multipart parts;
write-ops 200-only). Notably Kiota did **not** need the Functions `byte→binary` fix — it streams
octet-stream regardless — so that gap is NSwag-specific, not a shared-model defect.

**NEW — surfaced by the live test-run (`../kiota-testrun`), not by static analysis:** the model's
**list response shapes are wrong vs production**. The real Storage API returns **bare JSON arrays**:
```
GET /bucket                → [ {...} ]
POST /object/list/{bucket} → [ {...} ]
```
but the Smithy model wraps them (`structure ListBucketsOutput { @required items: BucketList }`), so
the generated `ListBucketsResponseContent` / `ListObjectsResponseContent` expose an `.Items` envelope
that **never matches**. The client deserializes the top-level array into an object → `.Items` is
`null` → returns **0 silently** (no error). This is the worst failure class — a green call with wrong
data — and it is **tool-independent** (NSwag emits the same envelope from the same model) and hits
**every SDK**. It is also a textbook "model not validated against production" defect: the hand-written
Smithy shape invented an envelope the server doesn't send. **Raise on `supabase/sdk`:** model the
list outputs as top-level lists (or fix the server contract). This is the most important gap found.

## Good / bad

**Good**
- **AOT/trimming-safe** (explicit `IParsable`, no reflection) — the standout.
- **Streaming works as-generated** (Stream in/out, MultipartBody) with no model patching.
- **Proper PascalCase** model names out of the box.
- **Properly factored transport** + mature middleware (retry/redirect/auth) in the runtime.

**Bad (for this SDK's wrapper architecture)**
- **8-package runtime dependency**, and it goes **public** (see below).
- **Models bleed through the public API.** Every model is
  `public partial class X : IAdditionalDataHolder, IParsable` exposing
  `Microsoft.Kiota.Abstractions.Serialization` types (`IParseNode`, `ISerializationWriter`).
  Returning one from a wrapper puts `Microsoft.Kiota.Abstractions` on your public surface → forced
  transitive dep on every consumer. **To stay Kiota-agnostic you must hand-write a full parallel
  DTO layer + mapping** — which erases the reason to generate models at all.
- **Un-ownable**: request-builder tree (51 files) + external runtime; you don't own the transport.
- **All-or-nothing**: can't cherry-pick models (they need the runtime) — no models-only adoption.
- **No domain error mapping**: generic typed errors; `FailureHint`-style reasons are rebuilt by hand.
- **Resists the partial seam** used to own product-specific behavior.

## Recommendation for supabase-csharp — **reject as an included artifact**
Kiota is the better-engineered client (AOT, streaming, factored transport) and would be the right
choice **only if** the team adopted a *whole* generated client + the Kiota runtime fleet-wide and
hand-wrote ergonomic wrappers over the builders. Under this SDK's model — generated code kept
**internal beneath a hand-written wrapper**, minimal/ownable dependencies — Kiota fails the decisive
test: **its models can't serve as your DTOs without leaking Kiota into the public API**, forcing a
full agnostic-DTO + mapping layer. That negates the one thing worth generating (ownable models).

See the root `codegen-comparison.md` for the head-to-head and the overall recommendation.

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

### 8. AOT / trimming — ✅ (no reflection)
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
`null` → returns **0 silently** (no error). This is a call that succeeds while returning wrong
data — the failure mode hardest to detect — and it is **tool-independent** (NSwag emits the same
envelope from the same model) and hits **every SDK**. It is a model-not-validated-against-production
defect: the modelled shape does not match what the server sends. **Raise on `supabase/sdk`:** model the
list outputs as top-level lists (or fix the server contract). This is the most important gap found.

**Update (July 2026):** fixed upstream in `supabase/sdk#55`. restJson1 cannot bind a list to the HTTP
payload, so `patch-openapi.py` unwraps the envelope in the generated OpenAPI; the model documents the
limitation. Verified by rerunning generation and the live harness against the corrected artifacts:
**4/4 pass** — `ListBuckets` returns real counts (the false-zero failure is gone) and `ListObjects`
counts the uploaded object. The same PR fixes the shared gaps listed above except **Auth**
(unmodelled) and the `200`-only write ops, which remain open.

## Good / bad

**Good**
- **AOT/trimming-safe** (explicit `IParsable`, no reflection) — the standout.
- **Streaming works as-generated** (Stream in/out, MultipartBody) with no model patching.
- **Proper PascalCase** model names out of the box.
- **Properly factored transport** + mature middleware (retry/redirect/auth) in the runtime.

**Bad (for this SDK's wrapper architecture)**
- **8-package runtime dependency**, and it goes **public** (see below).
- **Models reach the public API.** Every model is
  `public partial class X : IAdditionalDataHolder, IParsable` exposing
  `Microsoft.Kiota.Abstractions.Serialization` types (`IParseNode`, `ISerializationWriter`).
  Returning one from a wrapper puts `Microsoft.Kiota.Abstractions` on your public surface → forced
  transitive dep on every consumer. **To stay Kiota-agnostic you must hand-write a full parallel
  DTO layer + mapping** — which erases the reason to generate models at all.
- **External ownership**: request-builder tree (51 files) + external runtime; transport lives in
  the runtime, not in code the team can edit.
- **No models-only path**: the models require the runtime, so they cannot be adopted without it.
- **No domain error mapping**: generic typed errors; `FailureHint`-style reasons are rebuilt by hand.
- **Resists the partial seam** used to own product-specific behavior.

## Recommendation for supabase-csharp — **reject as an included artifact**
Kiota's client is AOT-safe, streams as generated, and delegates transport to a maintained runtime.
It fits **only if** the team adopts a *whole* generated client + the Kiota runtime fleet-wide and
hand-writes ergonomic wrappers over the builders — or for an SDK built from scratch, where there is
no owned transport to replace and no published API to break (see the root document's *Scope of
validity*). Under this SDK's model — generated code kept **internal beneath a hand-written
wrapper**, minimal dependencies — Kiota fails the decisive test: **its models cannot serve as the
SDK's DTOs without placing `Microsoft.Kiota.Abstractions` on the public API**, forcing a full
agnostic-DTO + mapping layer. That negates the one thing worth generating (usable models).

See the root [`codegen-comparison.md`](../codegen-comparison.md) for the full comparison, the
two-stage adoption strategy, and its conditions; this document is the tool-level evidence.

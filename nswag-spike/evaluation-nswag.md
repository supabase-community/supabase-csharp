# NSwag evaluation — answers to the spike brief (SDK-1107)

Tool: NSwag 14.7.1 (`openapi2csclient`), input = committed Smithy→OpenAPI from `supabase/sdk#51`.
Settings: `SystemTextJson, injectHttpClient, generateClientInterfaces, nullableReferenceTypes,
optionalParameters, generateDataAnnotations:false, SingleClientFromOperationId`.
All generated code compiles clean on netstandard2.1 (see `spike-nswag.csproj`).

Legend: ✅ works as-generated · 🟡 works only after a **model/patch** change (feeds back to
`supabase/sdk`) · ❌ not achievable with NSwag.

## Live run (`../nswag-testrun`) — what actually happened against a local platform

Running the generated client against `supabase start` (mirror of the Kiota run) was the most
informative step. Result: HttpClient injection ✅, **streaming upload ✅** (FileStream →
`FileParameter`/`StreamContent`, confirmed on the server), but **both list ops failed** — for two
distinct, serious reasons:

1. **NSwag serializes every optional field at its default (no `WhenWritingNull`).** Requests carry
   `file_size_limit:0`, `limit:0`, `offset:0`, `sortBy:null`, and the server rejects several:
   - `CreateBucket` sends `file_size_limit:0` → a bucket that **rejects every upload** (`413`). We
     had to set it explicitly to test uploads at all.
   - `ListObjects` sends `limit:0` (`400 "limit must be >= 1"`) and `sortBy:null`
     (`400 "sortBy must be object"`).
   This is the **non-nullable-optional data-fidelity flaw** (see §3a) turned into **functional
   breakage**: optional value types are non-nullable, default to `0`/`null`, and are always emitted.
   Kiota models the same fields nullable and omits them, so its identical calls just worked.
   *Fixable* (STJ `DefaultIgnoreCondition = WhenWritingNull` + nullable value types, or mark the
   fields nullable upstream) — but **the default generated output does not work against the server.**

2. **List responses fail to deserialize** — the shared model wraps arrays the API returns bare
   (`ListBuckets`/`ListObjects`; see §10). NSwag **throws** `ApiException("Could not deserialize…
   Status 200")` — notably **louder/safer than Kiota**, which silently returned an empty list for the
   same bug.

Net: the run confirms the headline capabilities (injection, streaming upload) *and* proves the
generated **request** DTOs are not usable as-is against Supabase without a serialization fix.

**Update (July 2026):** both defects were fixed upstream in `supabase/sdk#55` and verified by a
rerun against the corrected artifacts: list operations deserialize (bare arrays, direct
`ICollection<T>` returns), and `CreateBucket` works **without** `file_size_limit` — the spec now
types it `double?` and the server accepts `null` as unset, so the 413-bucket failure is gone.
`ListObjects` still returns 400: NSwag writes unset members as explicit nulls and the server
rejects `limit: null` (null-tolerance is per-field). The `WhenWritingNull` client configuration
therefore remains required — the generator-side half of the fix, unchanged.

## Question-by-question

### 1. Streaming uploads — ✅ (model gap, fixed upstream in `supabase/sdk#55`)
- **Multipart (`UploadObject`/`UpdateObject`): ✅ already streams.** Generated code builds
  `MultipartFormDataContent` + `new StreamContent(file.Data)` from a `FileParameter(Stream)` — no
  `byte[]` buffering of the file part.
- **Raw octet-stream body (`UploadChunk` TUS PATCH, Functions): ❌ as-shipped.** The body is a
  `$ref` to a `format: byte` payload schema, so NSwag **JSON-serializes it**
  (`JsonSerializer.SerializeToUtf8Bytes(body)` → `ByteArrayContent`) — non-streaming *and*
  semantically wrong (base64).
- **Proven fix (model side):** when the requestBody is **inline** `type: string, format: binary`
  (no `$ref`), NSwag emits `…Async(string fn, System.IO.Stream body, …)` + `new StreamContent(body)`
  — **true streaming, zero buffering.** So NSwag *can* do streaming uploads; the shared model must
  emit an inline binary body instead of a `$ref`-to-`format:byte` payload.
- **Fixed upstream** (`supabase/sdk#55`): the committed artifacts now emit inline binary for all
  octet-stream bodies; regeneration produces the `Stream` signatures with no local patching.

### 2. Streaming responses — ✅ as `Stream` (model gap, fixed upstream in `supabase/sdk#55`)
- **As-shipped: ❌.** Functions responses are `$ref` → `format: byte`, so NSwag returns
  `Task<byte[]>` read via `ReadObjectResponseAsync<byte[]>` — fully buffered + base64.
- **Proven fix:** flipping the response schema `format: byte → binary` makes NSwag emit
  `Task<FileResponse>`, where `FileResponse.Stream` is the **raw response stream**, read via
  `ReadAsStreamAsync` with `HttpCompletionOption.ResponseHeadersRead` — unbuffered streaming.
- **Caveat vs brief:** NSwag exposes a **`Stream`**, not `IAsyncEnumerable<byte[]>`. The brief
  accepts "`IAsyncEnumerable<byte[]>` **or** `Stream`", so this qualifies. `supabase/sdk#55`
  extends the byte→binary flip to Functions (and Database payloads); regeneration now yields
  `Task<FileResponse>` with no local patching. Live Functions verification remains open (needs a
  deployed edge function).

### 3. HttpClient injection — ✅
Constructor is `public StorageClient(System.Net.Http.HttpClient httpClient)` (same pattern for
`FunctionsClient`/`DatabaseClient`). Accepts an externally created/`IHttpClientFactory` client. No
internally-created `HttpClient`.

### 4. Middleware / handlers — ✅
Two seams: (a) the injected `HttpClient` carries any `DelegatingHandler` pipeline (auth headers,
error-relay inspection); (b) generated `partial void PrepareRequest(...)` / `ProcessResponse(...)`
hooks per request/response, implementable in a hand-owned partial. Auth headers (apikey/JWT/
`X-Client-Info`) are **not** modelled — they go here, hand-written.

### 5. Multipart uploads — ✅
Correct `MultipartFormDataContent` with explicit boundary and a streaming `StreamContent` file
part (see Q1). Non-file parts (`cacheControl`, `metadata`) are added as `StringContent`/
`ByteArrayContent`. Wart: they're generated as **required** (throw if null) because the injected
multipart schema marks them so — a model refinement, not a blocker.

### 6. Auth flows — ❌ (not modelled)
**Auth is absent from `supabase/sdk#51`.** Nothing to generate. Even once modelled, the
investigation stands: token/OTP/OAuth-redirect/session-refresh/MFA/PKCE are behavioral and
largely inexpressible in Smithy (the PR itself flags Auth as possibly unsuitable). Expected split:
**HTTP operations generatable; the session/refresh loop + PKCE + provider validation stay
hand-written.** Cannot be verified until models exist → **model gap to raise.**

### 7. PostgREST query builder — 🟡 minimal
Codegen produces: the 7 transport methods (`SelectRows`/`InsertRows`/… + RPC), `filters`/`args`
as `IDictionary<string,string>`, and a typed **`FilterOperator`** enum (all 24 operators). It does
**not** and cannot produce the fluent builder (`.Select()/.Eq()/.Order()`) — bodies are `byte[]`
(`Blob`, dynamic row shape). **The query builder + row typing stay entirely hand-written**; codegen
helps only at the enum + raw-transport layer. Matches the "PostgREST ~0%" prior finding.

### 8. AOT / trimming — ❌ as-generated
- Serialization is **reflection-based System.Text.Json** (`JsonSerializer.Deserialize<T>(…, options)`,
  13 call sites, **0** `JsonSerializerContext`/`[JsonSerializable]` source-gen). Not trim/NativeAOT
  safe without a hand-added source-gen context — **NSwag has no STJ source-gen emitter.**
- `ConvertToString` for enums uses `System.Reflection` (`GetDeclaredField` +
  `GetCustomAttribute<EnumMemberAttribute>`) — a secondary trim hazard.
- Verdict: needs post-processing (source-gen `JsonSerializerContext`) or a different tool to be
  AOT-clean. This is the weakest area for NSwag against the "idiomatic" bar.

### 9. Unity compatibility — 🟡 partial
Targets `netstandard2.1` → Unity-consumable, and it compiled clean. But the same reflection-STJ +
enum-reflection means **IL2CPP/AOT is not clean** for the same reasons as Q8. Framework-reachable,
not AOT-verified. (Consistent with the original investigation; treat as out-of-scope until Q8 is
solved, since the fix is the same.)

### 10. Model gaps found → raised on `supabase/sdk` (fixed in #55, except Auth and 200-only)
| Gap | Impact | Suggested model/patch fix |
|---|---|---|
| Functions/TUS request bodies are `$ref` → `format:byte` | streaming upload broken (JSON-serialized) | emit **inline** `type:string,format:binary` requestBody (no `$ref`) |
| Functions response `format:byte` | streaming response buffered/base64 | extend `patch-openapi.py` byte→binary to Functions responses |
| `{wildcardPath+}` greedy label | invalid C# identifier (`wildcardPath+`) → 24 compile errors | strip the RFC-6570 `+` in `patch-openapi.py`, or fix the `@httpLabel` emission |
| Multipart non-file parts marked required | forces non-null `cacheControl`/`metadata` | mark optional in the model |
| **Auth unmodelled** | can't evaluate/generate Auth at all | add Auth models (or formally scope it out as hand-written) |
| Write ops model `200` only | clients must tolerate 204 empty bodies | documented upstream; handle in hand-written seam |

**Update (July 2026):** the first four rows are fixed upstream in `supabase/sdk#55` (verified by
rerunning generation and the live harness against the corrected artifacts — no local `ready.json`
patching needed anymore). **Auth** and the `200`-only write ops remain open.

## Recommendation — **ADAPT** (models-first, hand-written operations/transport)

**Adopt generation for the low-behavior, high-drift layer; keep behavior hand-written.**

- **Generate (worth it):** wire **models/DTOs** (Storage `Bucket`/`FileObject`/signed-URL/response
  types; the `FilterOperator` enum). Irreducible data, where cross-SDK drift bites, near-zero
  wrapper cost. Concentrated in Storage (Functions/Database bodies are dynamic → nothing to model).
- **Do not generate (net-negative):** the **operation layer**. Each method is ~86 LOC of
  un-refactored transport that DRYs back into the SDK's existing `MakeRequest`, minus our
  `FailureHint` error mapping / serializer injection / dynamic headers. On an already
  thin-wrapper-shaped SDK, generated ops add a layer *beneath* a wrapper we write anyway.
- **Adopt one transport technique:** add `HttpCompletionOption.ResponseHeadersRead` to the shared
  `Core` transport.
- **Stays hand-written regardless:** Storage streaming/progress/TUS ergonomics, the PostgREST
  query builder, Functions method-dispatch, and **all of Auth**.

**Why not "adopt" (whole client):** streaming needs model fixes *and* still yields `Stream`/
`FileResponse` we'd wrap; AOT/trimming fails (reflection STJ); the operation layer is worse-factored
than what we own. **Why not "reject":** the model slice is a real, low-cost win *if* a validated
OpenAPI contract is maintained centrally (from whichever IDL emits it) — C# reuses it.
Standalone-for-C# only, the pipeline overhead (contract upstream, patches, naming template,
source-gen for AOT) likely exceeds the value.

**Net:** NSwag against these specs is a viable **models generator** and a poor **whole-client
generator**. Draw the line at models vs operations; feed the Q10 gaps back to `supabase/sdk` so
every SDK benefits.

The adoption strategy built on this verdict — two stages, drift monitoring, consumer impact of
integrating generated types into the published API, and the conditions/reversal criteria — is
defined in the root [`codegen-comparison.md`](../codegen-comparison.md); this document is the
tool-level evidence.

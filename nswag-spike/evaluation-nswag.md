# NSwag evaluation — answers to the spike brief (SDK-1107)

Tool: NSwag 14.7.1 (`openapi2csclient`), input = committed Smithy→OpenAPI from `supabase/sdk#51`.
Settings: `SystemTextJson, injectHttpClient, generateClientInterfaces, nullableReferenceTypes,
optionalParameters, generateDataAnnotations:false, SingleClientFromOperationId`.
All generated code compiles clean on netstandard2.1 (see `spike-nswag.csproj`).

Legend: ✅ works as-generated · 🟡 works only after a **model/patch** change (feeds back to
`supabase/sdk`) · ❌ not achievable with NSwag.

## Question-by-question

### 1. Streaming uploads — 🟡 (model gap, then ✅)
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

### 2. Streaming responses — 🟡 (model gap, then ✅ as `Stream`)
- **As-shipped: ❌.** Functions responses are `$ref` → `format: byte`, so NSwag returns
  `Task<byte[]>` read via `ReadObjectResponseAsync<byte[]>` — fully buffered + base64.
- **Proven fix:** flipping the response schema `format: byte → binary` makes NSwag emit
  `Task<FileResponse>`, where `FileResponse.Stream` is the **raw response stream**, read via
  `ReadAsStreamAsync` with `HttpCompletionOption.ResponseHeadersRead` — unbuffered streaming.
- **Caveat vs brief:** NSwag exposes a **`Stream`**, not `IAsyncEnumerable<byte[]>`. The brief
  accepts "`IAsyncEnumerable<byte[]>` **or** `Stream`", so this qualifies. `patch-openapi.py`
  already does this byte→binary flip for Storage; it must be **extended to Functions**.

### 3. HttpClient injection — ✅
Constructor is `public GoTrueGeneratedClient(System.Net.Http.HttpClient httpClient)`. Accepts an
externally created/`IHttpClientFactory` client. No internally-created `HttpClient`.

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

### 10. Model gaps found → to raise on `supabase/sdk`
| Gap | Impact | Suggested model/patch fix |
|---|---|---|
| Functions/TUS request bodies are `$ref` → `format:byte` | streaming upload broken (JSON-serialized) | emit **inline** `type:string,format:binary` requestBody (no `$ref`) |
| Functions response `format:byte` | streaming response buffered/base64 | extend `patch-openapi.py` byte→binary to Functions responses |
| `{wildcardPath+}` greedy label | invalid C# identifier (`wildcardPath+`) → 24 compile errors | strip the RFC-6570 `+` in `patch-openapi.py`, or fix the `@httpLabel` emission |
| Multipart non-file parts marked required | forces non-null `cacheControl`/`metadata` | mark optional in the model |
| **Auth unmodelled** | can't evaluate/generate Auth at all | add Auth models (or formally scope it out as hand-written) |
| Write ops model `200` only | clients must tolerate 204 empty bodies | documented upstream; handle in hand-written seam |

## Recommendation — **ADAPT** (models-first, hand-written operations/transport)

**Adopt generation for the low-behavior, high-drift layer; keep behavior hand-written.**

- **Generate (worth it):** wire **models/DTOs** (Storage `Bucket`/`FileObject`/signed-URL/response
  types; the `FilterOperator` enum). Irreducible data, where cross-SDK drift bites, near-zero
  wrapper cost. Concentrated in Storage (Functions/Database bodies are dynamic → nothing to model).
- **Do not generate (net-negative):** the **operation layer**. Each method is ~86 LOC of
  un-refactored transport that DRYs back into the SDK's existing `MakeRequest`, minus our
  `FailureHint` error mapping / serializer injection / dynamic headers. On an already
  thin-wrapper-shaped SDK, generated ops add a layer *beneath* a wrapper we write anyway.
- **Steal one idea:** add `HttpCompletionOption.ResponseHeadersRead` to the shared `Core` transport.
- **Unspillable, stays hand-written regardless:** Storage streaming/progress/TUS ergonomics, the
  PostgREST query builder, Functions method-dispatch, and **all of Auth**.

**Why not "adopt" (whole client):** streaming needs model fixes *and* still yields `Stream`/
`FileResponse` we'd wrap; AOT/trimming fails (reflection STJ); the operation layer is worse-factored
than what we own. **Why not "reject":** the model slice is a real, low-cost win *if* the org runs
the Smithy pipeline fleet-wide — C# just taps it. Standalone-for-C# only, the pipeline overhead
(Smithy upstream, patches, naming template, source-gen for AOT) likely exceeds the value.

**Net:** NSwag against these Smithy specs is a viable **models generator** and a poor **whole-client
generator**. Draw the line at models vs operations; feed the Q10 gaps back to `supabase/sdk` so
every SDK benefits.

# Smithy PR #51 review — what's generated, how, and from which source

> Reviewing `supabase/sdk#51` ("feat(smithy): add canonical Smithy models for APIs", author
> `grdsdev` / Guilherme Souza, branch `feat/smithy-models`, state OPEN, marked **DO NOT MERGE**).
> Context: other teams are exploring Smithy, which has **no C# emitter** — so the question is what
> this PR actually gives the C# SDK. Artifacts pulled into `_nswag-spike/smithy-pr51/`.

## Answers to the three questions

### From which source?
**Smithy IDL** (`smithy/model/*.smithy`) is the source of truth — hand-written models, *not*
derived from the existing SDK code or the live server. Originated in the **Swift spike**
([supabase-swift#1047](https://github.com/supabase/supabase-swift/pull/1047)) and promoted to the
shared `supabase/sdk` repo so every SDK team works from the same models. Covers **Storage,
Functions, Database (PostgREST)**. **Auth is explicitly not modelled yet** (PR flags OAuth
redirects + cookie session mgmt as possibly unsuitable for Smithy).

### How was it generated?
`smithy build` (AWS Smithy CLI, restJson1 protocol) → OpenAPI 3.0 → then a **`patch-openapi.py`
post-processor** fixes two things Smithy can't express:
1. `@streaming blob` emits `format: byte` (base64) → rewritten to `format: binary`.
2. No native `multipart/form-data` in Smithy → the script **injects** the `UploadObject`/
   `UpdateObject` multipart request bodies straight into the OpenAPI JSON.

The **patched OpenAPI files are committed** (`smithy/openapi/*.openapi.json`) precisely so
non-Smithy teams can consume them **without installing Smithy**. → **This is the C# path:
`Smithy → OpenAPI (committed) → NSwag/Kiota/TypeSpec`.** We never touch Smithy.

### What's generated? (and does it work for C#?)
I ran the committed **StorageService.openapi.json** through the same NSwag pipeline as the earlier
spikes. **Result: dramatically better — usable.**

| | Raw Storage `api.json` (Fastify) | Auth `openapi.yaml` (hand) | **Smithy PR#51 Storage** |
|---|---|---|---|
| operationIds | 0 | 0 | **20/20** |
| Named req/resp schemas | 3 | 15 | **26** |
| Method names | `S3GET4Async` | `VerifyPOST2Async` | **`ListBuckets`, `CreateSignedUrl`, `UploadObject`, `CreateTusUpload`** |
| Typed returns | 29/110 | partial | **all non-void ops** (`Task<ListBucketsResponseContent>`) |
| Surface | 110 ops (s3/iceberg/vector bloat) | ~60 | **20 ops, curated to real SDK** |
| Generated size | 13,671 LOC | 11,085 LOC | **2,889 LOC** |
| Compiles? | ✗ 214 errors | ✓ (after config) | **✓ after a 1-line patch** |

**The one blocker:** Smithy's greedy path label `{wildcardPath+}` carries the RFC-6570 `+`
modifier into the C# parameter name → `string wildcardPath+` (invalid identifier), 24 cascading
errors. A single `sed`/patch (`wildcardPath+` → `wildcardPath`, 10 occurrences) makes it
**compile clean (0 warnings/0 errors)**. This is a generic post-gen patch, same category as the
existing `patch-openapi.py` steps — trivial to fold in.

## What this settles vs. the prior spikes

Our two earlier findings were: (a) NSwag/C# is fine; (b) the *specs* were the ceiling (no
operationIds, unnamed bodies). PR #51 **removes that ceiling for Storage/Functions/PostgREST**:
because Smithy operations are named shapes, the generated OpenAPI has operationIds + named
schemas by construction, and NSwag then emits idiomatic, typed, compilable C#. The "spec quality"
experiment I proposed is effectively *already done upstream* — and it works.

So the earlier verdict updates:
- **Models + operation *signatures*: now genuinely generatable** for the 3 modelled services (was
  the open question). This is real, not hypothetical.
- **Boundaries unchanged / reconfirmed by the PR's own "Known Limitations":**
  - **Streaming / multipart / TUS** are *not* expressible in Smithy — they exist only because
    `patch-openapi.py` hand-injects them. So the transport for Storage's hard part is still
    manually authored, just relocated into a Python patch script rather than C#.
  - **PostgREST bodies are `Blob`** (row shape is dynamic) and the **query-builder (`.eq()`,
    `.like()`) stays hand-written by design** — matches our "PostgREST ~0% generatable" finding.
    Codegen gives you the transport skeleton + the `FilterOperator` enum, nothing more.
  - **Functions**: one Smithy op per HTTP method → **5 `InvokeFunction*` operations**; the client
    must hand-dispatch on `method` at runtime. The streaming-response feature is still net-new.
  - **Auth not modelled**, and called out as maybe-unsuitable — i.e. the largest hand-written
    service (5,497 LOC, the session/refresh/MFA/PKCE logic) is exactly what stays hand-written.
  - **Write ops model 200 only** (Smithy needs one success code) though PostgREST returns
    204/200 → generated clients must tolerate empty bodies (a correctness caveat to handle in the
    hand-written seam).

## On "should we try another toolchain?" — now with data
The README's C# suggestion is **Kiota or `@typespec/http-client-csharp`**, notably **not NSwag**.
But our spike shows **NSwag consumes the committed OpenAPI cleanly** (one patch) and emits plain,
ownable, System.Text.Json + injected-HttpClient code with **no runtime dependency** — which is
better aligned with "plain ownable OSS code" than Kiota (`Microsoft.Kiota.Abstractions` runtime
dep) or the TypeSpec emitter (`System.ClientModel`, Azure style). Recommendation stands: **NSwag
against these OpenAPI artifacts is the strongest C# option**; a Kiota bake-off is only warranted
if the org mandates it, and it starts at a dependency-footprint disadvantage.

## Suggested next steps
1. **Confirm the pipeline end-to-end** on Functions + Database OpenAPI too (Storage done here);
   expect the same `{wildcardPath+}`/path-label patch to be the only friction.
2. **Feed the `wildcardPath+` bug back to the PR** — it breaks every generator that isn't RFC-6570
   aware, so it belongs in `patch-openapi.py` (or fix the Smithy `@httpLabel` greedy-trait
   emission), not in each SDK's local workaround.
3. **Decide the C# boundary now** with real inputs: generate **models + operation signatures**
   from these OpenAPI files; keep **streaming/TUS, PostgREST query-builder, Functions dispatch,
   and all of Auth** hand-written on `Core`. This is the "A for models+transport-skeleton, B for
   behavior" hybrid, now backed by a compiling artifact.
4. **Raise Auth**: since it's unmodelled and the biggest hand-written surface, the C# spike's
   honest scope is "codegen helps Storage/Functions/PostgREST transport; Auth stays hand-written."

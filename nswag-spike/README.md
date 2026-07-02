# NSwag codegen spike (C# SDK · SDK-1107)

Evaluation of generating the C# SDK's transport + models from the shared Smithy models
(`supabase/sdk#51`), via the committed Smithy→OpenAPI artifacts and **NSwag**.

> ⚠️ **This is a spike, not shippable code.** Everything under `generated/` is machine-emitted
> and **not owned/maintainable**. It lives in its own standalone project (`spike-nswag.csproj`, a
> separate assembly) added to `Supabase.sln` for visibility, but it is **not referenced by
> `Supabase.csproj`** and has analyzers disabled — so it does **not** affect the product's build,
> analyzers, or quality metrics. Do not reference it from the SDK.

## Pipeline

```
Smithy model (supabase/sdk#51)  ──smithy build──►  *.openapi.json  ──NSwag──►  *Client.cs
   (hand-authored, upstream)        + patch-openapi.py                (generated, this spike)
```

We consume the **committed OpenAPI** (`smithy-pr51/*.openapi.json`); we never run Smithy in C#.

## Layout

| Path | What |
|------|------|
| `smithy-pr51/` | Upstream source: hand-written `*.smithy` models, the committed `*.openapi.json`, `patch-openapi.py`, upstream README. Copied from `supabase/sdk@feat/smithy-models`. |
| `generated/*.ready.json` | The OpenAPI actually fed to NSwag. Storage has one local patch: the greedy path label `{wildcardPath+}` → `{wildcardPath}` (the RFC-6570 `+` is not a valid C# identifier char). |
| `generated/*Client.cs` | NSwag output — Storage / Functions / Database clients + models. **Generated, non-owned.** |
| `spike-nswag.csproj` | Standalone project (in the solution, isolated) that compiles the three clients — proves they build (netstandard2.1, System.Text.Json only). |
| `evaluation-nswag.md` | **Per-toolchain evaluation** — what's produced, good/bad, answers to the brief. |
| `findings/` | Supporting memos: the Smithy PR review + the two raw-spec spikes. |

For the cross-toolchain comparison and overall recommendation, see the root
[`codegen-comparison.md`](../codegen-comparison.md).

## Reproduce

```bash
# tools: dotnet SDK + NSwag CLI + node (for the one-line wildcard patch)
dotnet tool install --global NSwag.ConsoleCore
# patch Storage's wildcard label, then generate each service:
nswag openapi2csclient /input:generated/StorageService.ready.json /classname:StorageClient \
  /namespace:Supabase.Storage.Gen /output:generated/StorageClient.cs \
  /jsonLibrary:SystemTextJson /injectHttpClient:true /generateClientInterfaces:true \
  /generateNullableReferenceTypes:true /generateOptionalParameters:true /useBaseUrl:false \
  /generateDataAnnotations:false /operationGenerationMode:SingleClientFromOperationId
# (Functions/Database identical, minus the wildcard patch)
# generateDataAnnotations:false → no System.ComponentModel.DataAnnotations dependency
dotnet build spike-nswag.csproj   # 0 warnings / 0 errors
```

## Conclusion (see `findings/` for the full case)

- **Models: worth generating.** Clean, typed, irreducible data (`Bucket`, `FileObject`, signed-URL
  responses, the `FilterOperator` enum). This is where cross-SDK drift bites and where generation
  removes real recurring work. Concentrated in **Storage** — Functions/Database bodies are dynamic
  (`Blob`/`byte[]`), so there are no meaningful models to generate there.
- **Operations: not worth it.** Each generated method is ~86 LOC of *un-refactored* transport —
  the same logic our shared `Helpers.MakeRequest` already does once, inlined per operation. Refactor
  it for DRY and it converges on our existing architecture; it also **lacks** our domain error
  mapping (`FailureHint`), serializer injection, and dynamic headers. The SDK is already
  thin-wrapper-shaped, so generated ops add a layer *beneath* a wrapper we write anyway.
- **The only transport idea worth stealing:** `HttpCompletionOption.ResponseHeadersRead` — a
  one-line addition to `Core`, no codegen required.
- **Unspillable by any generator:** Storage streaming/progress/TUS, the PostgREST query builder,
  Functions method-dispatch + streaming responses, and **all of Auth** (not modelled upstream).

**Net:** take generated **models** (Storage-first); keep operations + transport hand-written on a
shared `Core`. Whole-client generation is a worse-factored copy of what we already own.

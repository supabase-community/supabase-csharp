# Codegen toolchain comparison — supabase-csharp (SDK-1107)

Which toolchain, if any, should generate the HTTP layer of the C# SDK from the shared Smithy
models? This is the root summary; each toolchain has a detailed write-up in its spike folder.

- **NSwag** → [`nswag-spike/evaluation-nswag.md`](nswag-spike/evaluation-nswag.md) · project `spike-nswag`
- **Kiota** → [`kiota-spike/evaluation-kiota.md`](kiota-spike/evaluation-kiota.md) · project `spike-kiota`
- Background: [`nswag-spike/findings/`](nswag-spike/findings) (Smithy PR review + raw-spec spikes)

## The short version (read this, skip the tables)

We already have working transport. So the generated **client is a non-prize either way** — the
only thing worth taking from a generator is **models**. The two tools split precisely there:

- **Kiota — better client, solves nothing for us.** The client is genuinely well-built (AOT,
  streaming, factored transport). But its **models leak `Microsoft.Kiota.Abstractions` into our
  public API** (they implement `IParsable`). To adopt Kiota we'd have to wrap **two** layers — one
  over its client (which duplicates transport we already own) *and* one over its models (a full
  Kiota-agnostic DTO + mapping layer, just to not expose Kiota to developers). Two wrapping taxes,
  **zero net win.**

- **NSwag — horrendous client, but usable models.** The client is un-refactored duplication of the
  `MakeRequest` we already have — ignore it. But its **models are plain, dependency-agnostic POCOs**
  (`System.Text.Json` only) we can expose directly under the wrapper. That's the one real win.

**NSwag doesn't win by being good; it wins by being inert.** Its output is harmless enough to
cherry-pick (take the models, drop the client). Everything Kiota emits is entangled with its
runtime, so nothing can be cherry-picked.

**Therefore:** don't "adopt a generator." Use **NSwag in models-only mode** to generate the wire
DTOs (Storage models + `FilterOperator`), keep the hand-written transport + wrapper, and add a
source-gen `JsonSerializerContext` if AOT matters. Generate the boring data types; own everything
else. The tables below are the evidence; this paragraph is the conclusion.

## Method (shared across all toolchains)

- **Input:** the committed Smithy→OpenAPI artifacts from `supabase/sdk#51` (we consume the OpenAPI;
  we never run Smithy in C#). Same `*.ready.json` fed to every generator.
- **Target architecture:** the generated client is **internal**, sitting **beneath a hand-written
  ergonomic wrapper** (query builder, session loop, streaming/progress, auth). So the questions
  that matter are: are the *models* ownable and directly exposable, is the transport good, and does
  the generator's runtime leak into our public API.
- **"Idiomatic" bar:** `HttpClient`, System.Text.Json, `#nullable enable`, streaming up/down
  (`Stream`), .NET 6+/AOT/trimming, NuGet-clean, minimal/ownable dependencies.

## Toolchains evaluated

| Toolchain | Status | One-line verdict |
|---|---|---|
| **NSwag → CSharpClient** | ✅ run | Plain, zero-dep, ownable code; **models are exposable POCOs**. Weak on AOT (reflection STJ) and needs model patches for streaming — both fixable in-place. **Best fit.** |
| **Kiota (Microsoft)** | ✅ run | Better-engineered client: AOT-safe, streaming as-generated, factored transport. But 8-package runtime, request-builder tree, and **models leak Kiota into the public API**. Reject unless adopting the whole stack fleet-wide. |
| **@typespec/http-client-csharp** | ⏸ assessed, not run | Consumes **TypeSpec**, not OpenAPI → needs an OpenAPI→TypeSpec hop; emits Azure-style `System.ClientModel`-dependent code. Only worth it if the org mandates TypeSpec as SoT. |
| **OpenAPI Generator → csharp** | ⏸ assessed, not run | Default is RestSharp-based / non-idiomatic; `httpclient` library option exists but historically weaker than NSwag. A low-value third data point at best. |
| **Speakeasy** | ⏸ assessed, skipped | Commercial, account/API-key, vendor lock-in in an OSS pipeline. Skip absent org appetite. |
| **Smithy → C#** | ❌ excluded | No official C# emitter; would mean building one. |

Per the user's steer, the head-to-head is **NSwag vs Kiota**; the rest are parked with rationale.

## NSwag vs Kiota — head to head (same Storage input, both compile)

| Dimension | NSwag | Kiota |
|---|---|---|
| Output shape | 1 file, flat client + models (2,889 LOC) | request-builder tree, 51 files (4,338 LOC) |
| Call style | `client.ListBucketsAsync()` | `client.Bucket["id"].DeleteAsync()` |
| Model naming | `File_size_limit` (snake leaks) | **`FileSizeLimit`** ✅ |
| **Models are…** | **plain POCOs** (`[JsonPropertyName]` only) — exposable/liftable | `IParsable`+`IAdditionalDataHolder` — **runtime-coupled** |
| Runtime deps | **none** (System.Text.Json only) | **8 packages** (`Microsoft.Kiota.*` + `Std.UriTemplate`) |
| **AOT / trimming** | ❌ reflection STJ + enum reflection | ✅ **explicit `IParsable`, 0 reflection** |
| Streaming response | 🟡 needs `byte→binary` model patch → `FileResponse.Stream` | ✅ **native `Task<Stream>`** as-generated |
| Streaming upload | 🟡 needs inline-binary model patch → `StreamContent` | ✅ **native `Stream` / `MultipartBody`** as-generated |
| Transport factoring | inlined per op (~86 LOC), un-refactored, **owned** | factored into the Kiota runtime, **not owned** |
| Domain error mapping | wrap `ApiException<T>` → keep `FailureHint` | generic typed errors; rebuild reasons by hand |
| Middleware seam | `DelegatingHandler` + `PrepareRequest`/`ProcessResponse` partials | rich handler pipeline, but tree **resists partial seam** |
| Ownership / walk-away | **plain code, fully ownable** | coupled to Kiota runtime + generated tree |
| Public-API bleed | none (STJ is a base dep) | **yes — models put `Kiota.Abstractions` on your public surface** |

## The two findings that decide it

1. **Only models are worth generating.** Operations are either un-refactored duplication of our
   `MakeRequest` (NSwag) or well-factored into an external runtime we don't own (Kiota); the
   ergonomic layer (query builder, streaming/progress/TUS, session loop, Auth) is hand-written
   either way. So the prize is **ownable, directly-exposable model DTOs.**

2. **The wrapper test picks the tool.** Under "generated client internal, hand-written wrapper on
   top", the wrapper's return types are the bleed vector:
   - **NSwag models are plain POCOs** → expose directly (or map trivially to fix naming), no
     third-party bleed.
   - **Kiota models implement `IParsable`** → returning one forces `Microsoft.Kiota.Abstractions`
     onto your public API as a transitive dep on every consumer. To avoid it you must hand-write a
     **full agnostic DTO + mapping layer** — which erases the benefit of generating models. And you
     can't cherry-pick Kiota's models (they need the runtime), so there's no models-only path.

Kiota's genuine advantages (AOT, streaming, names) are real but only materialise if you adopt the
*entire* Kiota stack — the opposite of the internal-generated + ownable-wrapper design.

## TypeSpec (`@typespec/http-client-csharp`) — assessed, not pursued

"TypeSpec" in the brief hides two different things:

1. **`TypeSpec → @typespec/openapi3 → Kiota`** — this only uses TypeSpec to *emit OpenAPI*, then
   runs Kiota. We already have OpenAPI (from Smithy), so the TypeSpec step is redundant and the
   Kiota half is done above. This row collapses to **Kiota**.
2. **`@typespec/http-client-csharp`** — the actual distinct toolchain (Microsoft's official
   TypeSpec C# emitter, Azure-SDK lineage). Two reasons it doesn't fit, the second decisive:
   - **Input mismatch.** It consumes **TypeSpec**, not OpenAPI. Our source of truth is
     **Smithy → OpenAPI**, so we'd need a fragile, lossy OpenAPI→TypeSpec conversion hop that fights
     the pipeline the org actually chose.
   - **Same bleed as Kiota, different runtime.** It emits whole clients on **`System.ClientModel`**,
     and its **models implement `System.ClientModel`'s serialization interfaces (`IJsonModel<T>`)** —
     i.e. runtime-coupled models that leak `System.ClientModel` onto our public API exactly like
     Kiota leaks `Kiota.Abstractions`. It's **"Kiota with a different logo"**: better-engineered
     client, AOT-decent, and models we still can't expose without a full agnostic-DTO remap.

**Verdict:** not worth pursuing. It reproduces the Kiota outcome (coupled models → no models-only
path → no win under a wrapper) *and* adds an input-format hop. The only scenario that revives it is
the **org mandating TypeSpec as the fleet source of truth** — and even then the C# answer is
unchanged: emit OpenAPI via `@typespec/openapi3` and feed **NSwag models-only**; you still would not
adopt the TypeSpec *client* emitter.

## Why we're stopping the tool bake-off

Every OpenAPI→C# generator sorts into one of two families, and we've now sampled both:

- **Whole-client generators** — Kiota, `@typespec/http-client-csharp`, OpenAPI-Generator's default:
  runtime-coupled models → public-API bleed → no models-only path.
- **DTO/POCO generators** — NSwag (and, if ever needed, OpenAPI Generator's models are also inert
  POCOs): usable, exposable models.

The remaining named options don't open a new lane: **OpenAPI Generator (csharp)** gives inert models
like NSwag but a worse (RestSharp/`generichost`) client — no gain over NSwag models-only;
**Speakeasy** is commercial lock-in; **Refit/Refitter** is a different paradigm (Refit-attributed
interfaces + a Refit runtime dep), not better for our narrow need; **Smithy-native C#** has no
emitter. None changes the verdict, because the verdict is **architectural, not tool-specific**:
client generation is a dead end (we own transport), and only inert POCO models are worth taking. The
one thing that *would* be genuinely different is abandoning off-the-shelf generators for **in-house
templating / Roslyn source-gen scaffolding** — a *build vs buy* decision, not another tool to trial.

## Recommendation — **ADAPT, with NSwag, models-only**

- **Generate** wire **models/DTOs** with **NSwag** (Storage `Bucket`/`FileObject`/signed-URL/
  response types; the `FilterOperator` enum). Exposable POCOs that fit under the wrapper; where
  cross-SDK drift bites; near-zero wrapper cost.
- **Hand-write** operations, transport, streaming/TUS ergonomics, the PostgREST query builder,
  Functions dispatch, and **all of Auth** on the owned `Core`.
- **Close NSwag's one real gap in-place:** add a source-gen `[JsonSerializable] JsonSerializerContext`
  for the generated models (AOT/trim-safe STJ) and take enum `ConvertToString` off reflection. This
  neutralises Kiota's headline advantage while keeping plain, zero-dep, ownable code.
- **Steal one transport idea regardless:** `HttpCompletionOption.ResponseHeadersRead` in `Core`.
- **Reject Kiota** for inclusion: better client, wrong ownership model, and its models can't serve
  our public API without a redundant agnostic-DTO layer.
- **Feed model gaps upstream** to `supabase/sdk` (streaming `byte→binary`, required multipart parts,
  Auth). Details in the per-toolchain evals.

**Conditions that would change this:** the org mandates TypeSpec as SoT (evaluate
`@typespec/http-client-csharp`), or the team decides to stop owning transport entirely and adopt a
whole generated client + runtime fleet-wide (Kiota becomes viable).

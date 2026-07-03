# Codegen toolchain comparison — supabase-csharp (SDK-1107)

This document evaluates which toolchain, if any, should generate the HTTP layer of the C# SDK from
the shared contract models. It is the root summary; each toolchain has a detailed write-up in its
spike folder. Findings, measurements, and version numbers are as of July 2026.

- **NSwag** → [`nswag-spike/evaluation-nswag.md`](nswag-spike/evaluation-nswag.md) · project `spike-nswag`
- **Kiota** → [`kiota-spike/evaluation-kiota.md`](kiota-spike/evaluation-kiota.md) · project `spike-kiota`
- Background: [`nswag-spike/findings/`](nswag-spike/findings) (Smithy PR review + raw-spec spikes)

## Executive summary

The SDK ships working, hand-written transport, and its model types are published public API on
NuGet. Evaluated against that baseline, neither generated client improves on the current code;
generated models add value, but not by replacing existing types in the short term. The two
toolchains that were run differ as follows:

- **Kiota.** The generated client is AOT-safe, streams as generated, and delegates transport to a
  runtime. Adopting it would replace maintained, working transport with 4,338 generated LOC plus an
  8-package runtime (see *Cost analysis*). Its models implement `IParsable`, which places
  `Microsoft.Kiota.Abstractions` on the SDK's public API; keeping the public API free of Kiota
  types would require a hand-written DTO and mapping layer in addition to the wrapper, which
  removes the benefit of generating the models.

- **NSwag.** The generated client duplicates the per-operation transport the SDK already implements
  in `MakeRequest` and adds no capability. Its models are plain POCOs with no dependency beyond
  System.Text.Json and can be used without the client or any NSwag runtime.

The decisive property is separability: NSwag's models can be adopted without its client; Kiota's
output cannot be split from its runtime.

Because the SDK is a published package, exposing generated models in place of the existing public
types is a breaking change, and mapping between generated and exposed types would erase most of the
benefit of generating them (see *Consumer impact*). The near-term value of generated models is
therefore not replacement but: (1) detecting and prioritising drift between the SDK and the shared
contract, and (2) providing the model types for new endpoints as they are implemented.

**Recommendation, in two stages:** *Stage 1 (now)* — generate models with NSwag in models-only
mode; use them as the type source for newly implemented endpoints and as a drift monitor against
the existing public types; change no existing public API. *Stage 2 (next major version)* — decide
whether generated types replace the existing ones, bundled with the breaking changes already
planned for the feature-parity catch-up, and conditional on the contract source having proven
reliable. Details, conditions, and reversal criteria follow.

This recommendation is specific to an SDK with owned working transport, a published public API,
and a feature-parity backlog. For an SDK built from scratch, the same evaluation produces a
different answer; *Scope of validity* states which constraints drive the conclusion and what
changes without them.

## Scope and method

- **Input:** the committed Smithy→OpenAPI artifacts from `supabase/sdk#51`. The C# SDK consumes the
  OpenAPI output; Smithy is never run in the C# toolchain. The same `*.ready.json` files were fed
  to every generator.
- **Target architecture:** any generated client is internal, placed beneath a hand-written
  ergonomic wrapper (query builder, session loop, streaming/progress, auth). The evaluation
  questions follow from this: can the models be owned and exposed directly, is the transport an
  improvement, and does the generator's runtime reach the public API.
- **Idiomatic bar:** `HttpClient`, System.Text.Json, `#nullable enable`, streaming in both
  directions (`Stream`), .NET 6+/AOT/trimming, NuGet-clean packaging, minimal dependencies.
- **Determinism:** all toolchains evaluated are template-based generators whose output is a
  function of the input document and configuration only; regeneration is reproducible and involves
  no generative-AI component.

## Toolchains evaluated

| Toolchain | Status | Assessment |
|---|---|---|
| **NSwag → CSharpClient** | ✅ run | Plain, dependency-free output; models are usable POCOs. Requires fixes for serialization defaults, AOT (reflection-based serialization), and naming; all are local changes. Recommended, in models-only mode. |
| **Kiota (Microsoft)** | ✅ run | Client is AOT-safe, streams as generated, delegates transport to a runtime. Requires an 8-package runtime; models expose Kiota types on the public API. Not recommended unless a whole generated client and runtime are adopted fleet-wide. |
| **@typespec/http-client-csharp** | ⏸ assessed, not run | Consumes TypeSpec, not OpenAPI, so it would require an OpenAPI→TypeSpec conversion step; emits `System.ClientModel`-dependent code. Relevant only if the organisation adopts TypeSpec as source of truth. |
| **OpenAPI Generator → csharp** | ⏸ assessed, not run | Default output is RestSharp-based; an `httpclient` option exists. Assessed as adding no capability beyond NSwag's models. |
| **Speakeasy** | ⏸ assessed, not run | Commercial; requires an account and API key; introduces vendor lock-in into an OSS pipeline. |
| **Smithy → C#** | ❌ excluded | No official C# emitter exists; this route would require building one. |

NSwag and Kiota were run and are compared in detail below; the remaining toolchains were assessed
and not pursued for the reasons stated above and in *Coverage of the remaining options*.

## Evaluation against the current SDK

The baseline is the hand-written client in production. For Storage, the service used as generator
input, the current implementation is 2,511 LOC: `StorageFileApi.cs` (907), `StorageBucketApi.cs`
(155), ~600 LOC of streaming-progress and upload-caching ergonomics, interfaces and option types,
and ~200–260 LOC of wire DTOs (`Bucket`, `FileObject`, `FileObjectV2`, `UploadSignedUrl`,
`SearchOptions`, request options). For GoTrue: `Client.cs` <1k, `Api.cs` ~800. This code is
maintained end-to-end by the team, with `FailureHint` error mapping and no third-party runtime
dependency. Each generated artifact, judged against that baseline:

| Generated artifact | Improvement over current code? | Reason |
|---|---|---|
| **Kiota client** | No | Provides AOT-safe serialization and streaming as generated, but replaces maintained, working transport with 4,338 generated LOC plus an 8-package runtime, provides none of the current ergonomics (upload progress, caching, `FailureHint` mapping), and still requires the wrapper to be hand-written on top. |
| **NSwag client** | No | Re-implements the `MakeRequest` transport the SDK already has, without refactoring and without the current error mapping, serializer injection, and dynamic headers. |
| **Kiota models** | No | Runtime-coupled (`IParsable`): exposing them places `Kiota.Abstractions` on the public API, a dependency the current hand-written models do not impose. |
| **NSwag models** | Yes, conditional | Plain POCOs, equivalent in shape to the current hand-written DTOs, but generated from the shared contract, which protects against cross-SDK drift. As the SDK's model types are published API, the integration path matters as much as the artifact: new endpoints and drift monitoring first, replacement only at a major version (see *Consumer impact*). |
| **Transport techniques** | One | `HttpCompletionOption.ResponseHeadersRead` is applicable to `Core` independently of any adoption decision. |

Adoption is per artifact, not per tool: a client can be rejected while its models, or a single
technique, are adopted.

## NSwag vs Kiota — observed properties (same Storage input, both compile)

| Dimension | NSwag | Kiota |
|---|---|---|
| Output shape | 1 file, flat client + models (2,889 LOC) | request-builder tree, 51 files (4,338 LOC) |
| Call style | `client.ListBucketsAsync()` | `client.Bucket["id"].DeleteAsync()` |
| Model naming | `File_size_limit` (snake_case retained) | `FileSizeLimit` (PascalCase) |
| Model coupling | plain POCOs (`[JsonPropertyName]` only), usable standalone | `IParsable` + `IAdditionalDataHolder`, coupled to the Kiota runtime |
| Runtime dependencies | none (System.Text.Json only) | 8 packages (`Microsoft.Kiota.*` + `Std.UriTemplate`) |
| AOT / trimming | ❌ reflection-based System.Text.Json + enum reflection | ✅ explicit `IParsable`, no reflection |
| Streaming response | 🟡 requires a `byte→binary` model patch → `FileResponse.Stream` | ✅ `Task<Stream>` as generated |
| Streaming upload | 🟡 requires an inline-binary model patch → `StreamContent` | ✅ `Stream` / `MultipartBody` as generated |
| Transport placement | inlined in the generated code (~86 LOC per operation), no runtime dependency; regenerated on each run | in an external 8-package runtime; generated code only builds requests |
| Domain error mapping | wrap `ApiException<T>`, retaining `FailureHint` | generic typed errors; domain reasons rebuilt by hand |
| Middleware seam | `DelegatingHandler` + `PrepareRequest`/`ProcessResponse` partials | handler pipeline (retry, redirect, auth); the builder tree does not offer a partial-class seam |
| Suitability for wrapping | flat client + POCO returns; direct to wrap and re-expose | builder tree designed for direct use; extension happens through the handler pipeline |
| Dependency maintenance (as of July 2026) | community project led by Rico Suter with contributors; actively maintained, tracks .NET 10 (v14.7.1). In models-only mode no NSwag runtime ships, so tool abandonment does not affect built output | Microsoft-owned; actively maintained (kiota-dotnet 2.0.0, May 2026); used by the Microsoft Graph SDKs. The runtime ships as a permanent dependency of the SDK |
| Adoption as a whole client | inline transport in generated code, wrapped by hand | factored runtime + builder tree, wrapped by hand |
| Adoption as models only | ✅ POCOs usable without the client | ❌ not possible; models require the runtime |
| Public-API exposure | none (System.Text.Json is a base dependency) | models place `Kiota.Abstractions` on the public surface |

## Live-run results (`kiota-testrun`, `nswag-testrun`)

Both generated clients were run against a local platform (`supabase start`). The runs exercised
Storage operations; the Functions and Database clients were generated and compiled but not
exercised against the platform. Both runs confirmed that HttpClient injection and streaming upload
work. They also surfaced defects that static analysis had not, with different failure modes per
tool:

- **Shared model defect (both tools):** the list endpoints return bare JSON arrays, but the model
  wraps them in an envelope (`{items:[]}`). Kiota deserializes the mismatch into an object whose
  `Items` is null and returns an empty result with no error; NSwag throws an `ApiException` on
  deserialization. The same upstream defect therefore produces a silent wrong result in one tool
  and an explicit failure in the other.
- **NSwag-specific defect:** NSwag serializes every optional field at its default value
  (`file_size_limit:0`, `limit:0`, `sortBy:null`), and the server rejects several of these: a
  bucket created with `file_size_limit:0` rejects all uploads with 413; `limit:0` and
  `sortBy:null` return 400. Kiota omits unset fields, so the equivalent calls succeeded. NSwag's
  request DTOs are therefore not usable as generated; they require a serialization fix
  (`WhenWritingNull` plus nullable value types). Details are in each `evaluation-*.md`.

**Cause of the divergence, given the same spec.** The OpenAPI marks these fields optional but not
`nullable`, leaving optionality under-specified; the two generators fill the gap with opposite
defaults. Kiota maps optional value types to `double?` and omits nulls on serialization, producing
minimal, valid bodies. NSwag maps them to non-nullable `double` (default `0`) and serializes every
field, producing bodies the server rejects. This is a difference in generator defaults against an
under-specified schema, not an intrinsic property of either tool, and the NSwag side is fixable
(mark the fields `nullable` upstream, or configure `DefaultIgnoreCondition = WhenWritingNull` with
nullable value types).

Model quality therefore has two independent axes, and the tools divide across them:

| Axis | Favours | Reason |
|---|---|---|
| Usability under a wrapper (ownership, exposability) | NSwag | plain POCOs; Kiota's `IParsable` models expose the runtime on the public API |
| Request-serialization correctness as generated | Kiota | nullable mapping + omitted nulls; NSwag serializes unset defaults the server rejects |

These two results differ in kind: Kiota's coupling is structural (there is no plain-model output),
while NSwag's serialization gap is a configuration and annotation fix. NSwag can be made correct
while remaining dependency-free; Kiota's models cannot be decoupled from its runtime.

A further conclusion applies regardless of tool: generated models are only as correct as the source
model. The defects found by the live runs are located in the shared contract model — a work in
progress at the time of this spike — not in the generators, which both translated their input
faithfully and reproducibly. That determinism works in the pipeline's favour: a defect exists once,
in the model, and its fix propagates identically to every SDK on regeneration, where the equivalent
defect in hand-written SDKs would be introduced and fixed per SDK. The corollary is that validation
must target the model: the API evolves, so model accuracy is a state to maintain rather than reach
once, and a live contract test of this kind should be a permanent stage in any adopted pipeline —
it validates the model, not the generators.

## Cost analysis — generated vs. hand-maintained code

For Storage, the like-for-like sizes are: 2,511 hand-written LOC today, 2,889 generated by NSwag,
4,338 generated by Kiota. The LOC parity with NSwag overstates the generated client's coverage: the
hand-written 2,511 includes upload-progress reporting, upload caching, and `FailureHint` error
mapping, none of which either generated client provides. Generated code is not authored by the team
but is not cost-free; the comparison is between unowned generated code plus a pipeline, and owned
code the team can edit directly.

| | Generate (whole client) | Hand-maintain (current) |
|---|---|---|
| LOC | 4,338 (Kiota) / 2,889 (NSwag), not authored by the team | 2,511 (Storage), authored by the team, including ergonomics the generated clients lack |
| Fixing a defect | regenerate, or work around it; defects in Kiota's runtime require an upstream fix | edit in place and ship |
| Standing cost | contract-model upstream + OpenAPI patches + naming/AOT configuration + regeneration on drift + (Kiota) the runtime's dependency lifecycle | ongoing reading, debugging, and manual handling of drift |
| Defects observed in the live run | envelope and serialization-default defects, located in generated code and fixed through the model or configuration rather than the code itself | not exercised |
| Readability / debugging | requires understanding the generator's output and, for Kiota, its runtime | already understood by the team |

The slice where generation is justified is small and measurable: the wire DTOs amount to ~200–260
LOC in today's Storage implementation. Generating them is not a labour saving at that size; the
benefit is that they derive from the shared contract, which is what protects against cross-SDK
drift. For the client, generation replaces a similar-sized, owned, working implementation (with
more functionality) by a larger, unowned one; whether that trade is acceptable is a build-versus-buy
decision that tips toward generation only if the organisation operates the contract pipeline
fleet-wide, so that the C# SDK reuses existing infrastructure. For C# in isolation, hand-maintaining
the owned surface is the cheaper option.

## Consumer impact — integrating generated models into a published SDK

The SDK's model types (`Bucket`, `FileObject`, …) are published public API. There are three ways to
integrate generated models, with different costs:

- **(a) Replace the existing types.** A breaking change requiring a revalidation pass. Its cost
  depends on the vehicle: as a standalone break it is the most expensive option; bundled into an
  already-planned major version it is marginal, since revalidation is paid per major release, not
  per change. The SDK's feature-parity backlog implies such a major version is coming. The scope of
  revalidation can also be bounded: with naming and namespaces aligned, replacement is largely a
  type-identity change with unchanged behaviour, and the semantic risk concentrates in the
  nullability fix (`int` → `int?` on optional fields), which changes signatures and behaviour.
- **(b) Map between generated and exposed types.** A hand-written mapping layer. This is the same
  cost identified for Kiota's models, and it removes most of the benefit of generating models.
- **(c) Integrate at the edges and monitor.** Use generated models where new endpoints introduce
  new types, and diff generated models against the existing public types to detect drift. No
  existing API changes.

Option (c) carries a hybrid risk — generated and hand-written types coexisting in one package. Two
rules bound it: extend an existing type when a new endpoint touches one, rather than introducing a
parallel generated type; and treat coexistence as a transition that converges at the next major
version, not as a permanent state.

**Drift monitoring is a report, not a build gate.** The SDK is currently behind the contract on
feature parity, so divergence between generated and existing types is expected and cannot fail a
build without blocking unrelated work. The workable design is a committed baseline of the current
diff, with only new deltas surfaced, triaged into two classes:

1. **Divergence on implemented surface** — the SDK exposes the endpoint/type but its shape
   disagrees with the contract: an SDK defect or an upstream contract change; high priority.
2. **Contract surface with no SDK counterpart** — endpoints or fields the SDK has not implemented:
   input to the feature-parity backlog and its prioritisation.

This makes the generated models useful immediately — as a drift detector and a backlog signal —
while deferring all public-API decisions to a major version.

## Key findings

1. **Models are the only layer where generation adds value.** The generated operation layers either
   duplicate the existing `MakeRequest` transport without refactoring (NSwag) or move transport
   into an external runtime (Kiota); the ergonomic layer (query builder, streaming/progress/TUS,
   session loop, Auth) is hand-written in every scenario. The candidate artifact is the set of
   model DTOs.

2. **The wrapper architecture determines the tool.** With a generated client kept internal beneath
   a hand-written wrapper, the wrapper's return types are where a generator's runtime can reach the
   public API:
   - NSwag models are plain POCOs and can be used directly, with no third-party dependency.
   - Kiota models implement `IParsable`; returning one places `Microsoft.Kiota.Abstractions` on the
     public API as a transitive dependency for every consumer. Avoiding this requires a hand-written
     DTO and mapping layer, which removes the benefit of generating models; and because Kiota's
     models require its runtime, there is no models-only adoption path.

3. **In a published SDK, the integration path outweighs the artifact.** Generated models deliver
   near-term value as a drift monitor and as the type source for new endpoints; replacing existing
   public types is a major-version decision whose cost depends on what it is bundled with.

Kiota's capabilities (AOT safety, streaming as generated, PascalCase naming) apply only when the
entire Kiota stack is adopted, which is the opposite of the internal-client, owned-wrapper design.

## TypeSpec (`@typespec/http-client-csharp`) — assessed, not pursued

The TypeSpec option covers two distinct routes:

1. **`TypeSpec → @typespec/openapi3 → <generator>`** — `@typespec/openapi3` emits an OpenAPI
   document only; it does not select the C# generator. Given an OpenAPI document, the generator
   choice is the same one evaluated above. The SDK already receives OpenAPI from the contract
   pipeline, so this route produces no input that is not already available.
2. **`@typespec/http-client-csharp`** — the distinct toolchain (Microsoft's TypeSpec C# emitter,
   Azure-SDK lineage). Two findings, the second decisive:
   - **Input mismatch.** It consumes TypeSpec, not OpenAPI. The contract pipeline under evaluation
     emits OpenAPI, so this route would require a lossy OpenAPI→TypeSpec conversion step running
     counter to that pipeline.
   - **Runtime-coupled models, as with Kiota.** It emits whole clients on `System.ClientModel`, and
     its models implement `System.ClientModel`'s serialization interfaces (`IJsonModel<T>`). The
     models therefore expose `System.ClientModel` on the public API in the same way Kiota's models
     expose `Kiota.Abstractions`, and cannot be exposed without a separate DTO and mapping layer.

**Assessment:** not pursued. It reproduces the Kiota outcome (runtime-coupled models, no
models-only path) and adds an input-format conversion. It would become relevant only if the
organisation adopted TypeSpec as the fleet source of truth — and in that case the C# conclusion is
unchanged: emit OpenAPI via `@typespec/openapi3` and generate models with NSwag; the TypeSpec
client emitter would still not fit the wrapper architecture.

## Coverage of the remaining options

OpenAPI→C# generators fall into two families, and both have been sampled:

- **Whole-client generators** — Kiota, `@typespec/http-client-csharp`, OpenAPI Generator's default:
  runtime-coupled models, public-API exposure, no models-only path.
- **DTO/POCO generators** — NSwag (OpenAPI Generator's models are also plain POCOs): models usable
  independently of the client.

The remaining named options do not add a third family: OpenAPI Generator (csharp) produces plain
models comparable to NSwag's with a RestSharp- or `generichost`-based client, adding no capability
over NSwag in models-only mode; Speakeasy is commercial with vendor lock-in; Refit/Refitter is a
different paradigm (Refit-attributed interfaces plus a Refit runtime dependency) that does not
address the models-only requirement; Smithy-native C# has no emitter. Further trials would not
change the conclusion, because the conclusion is architectural rather than tool-specific: client
generation replaces owned working transport, and only plain POCO models fit the wrapper
architecture. The one alternative of a different kind is in-house templating or Roslyn source
generation — a build-versus-buy decision rather than another tool to trial.

## Recommendation — adapt, with NSwag in models-only mode, in two stages

### Stage 1 — now; no change to existing public API

- **Set up NSwag models-only generation** from the committed OpenAPI, with the three fixes required
  before any generated type ships (the first two surfaced by the live runs):
  - *Serialization correctness* — configure `DefaultIgnoreCondition = WhenWritingNull` and make
    optional value types nullable, so requests stop sending `file_size_limit:0` / `limit:0` /
    `sortBy:null`. Without this the request DTOs do not work against the server (see *Live-run
    results*).
  - *AOT/trimming* — add a source-generated `[JsonSerializable] JsonSerializerContext` for the
    generated models and replace the reflection-based enum `ConvertToString`.
  - *Naming* — map generated names to PascalCase (generator configuration or a post-processing
    step); `File_size_limit` does not meet the public-API bar.
- **Use generated models for new endpoints** implemented during the feature-parity catch-up,
  extending existing types where an endpoint touches one rather than introducing parallel types.
- **Stand up the drift monitor** described in *Consumer impact*: committed baseline, new deltas
  reported and triaged into defects (implemented surface) and backlog input (unimplemented
  surface).
- **Adopt `HttpCompletionOption.ResponseHeadersRead`** in `Core`, independently of the above.
- **Do not adopt Kiota:** its client replaces owned, working transport with an unowned builder tree
  plus runtime, without improving on the current code, and its models cannot serve the public API
  without a redundant DTO and mapping layer.
- **Report model gaps upstream** to `supabase/sdk` — the shared model, not the tool choice, is the
  larger risk. Priority order (all found or confirmed by the live runs):
  1. **List-response envelope** — the model wraps arrays the API returns bare
     (`ListBuckets`/`ListObjects`); Kiota returns empty results silently, NSwag throws. Affects
     every SDK.
  2. **Optional fields not marked `nullable`** — leads generators to serialize defaults the server
     rejects. Marking them nullable fixes this for every SDK.
  3. Streaming `byte→binary`; required multipart parts; and Auth, which is not modelled.

### Stage 2 — at the next major version

- **Decide whether generated types replace the existing public model types**, bundled with the
  breaking changes already planned for the feature-parity catch-up, so that the break and the
  revalidation pass are paid once.
- Preconditions: the contract source has proven reliable through Stage-1 monitoring, and the
  upstream gaps above are fixed. If they are not, Stage 1 continues as-is — the drift monitor
  remains a prioritisation tool and no public API changes.

## Scope of validity

The recommendation follows from three facts about this SDK's current position, not from properties
of code generation itself:

- transport exists, works, and is owned — a generated client replaces working code rather than
  adding capability;
- the model types are published public API — exposing generated types is either a breaking change
  or a mapping layer;
- the SDK is behind the contract on feature parity — divergence from the specification is backlog,
  not regression.

For an SDK built from scratch, none of these constraints exists: there is no transport to replace,
no published API to break, and no accumulated drift. Under those conditions the same evaluation
produces a different answer — whole-client generation becomes the natural starting point, and
Kiota is the strongest candidate assessed here (AOT-safe, streaming as generated, Microsoft-
maintained runtime), with its runtime dependency accepted as a day-one design decision rather than
introduced as a migration cost.

Two findings carry over to a greenfield setting unchanged, with greater weight:

1. **Generated code is only as correct as the contract it is generated from.** A greenfield SDK
   has no hand-written baseline to diff against, so silent model defects of the kind found in the
   live runs (list-envelope, nullability) would ship directly to production.
2. **A live contract-test harness is therefore a required pipeline stage from the first release**,
   not a hardening step added later.

The conclusions in this document should not be applied to other SDKs or to new SDKs without
re-running the evaluation against their own baselines.

## Conditions, risks, and deliverables

**Deliverables.**

1. The NSwag models-only pipeline with the three fixes (serialization, AOT, naming).
2. The drift monitor: generated models diffed against public types, committed baseline, two-class
   triage.
3. Upstream fixes to the shared model (nullability, list envelopes, streaming, Auth) — the
   higher-leverage work, benefiting every SDK.
4. A live contract-test harness (`*-testrun`) as a permanent pipeline stage — the mechanism that
   catches model-versus-production drift before it ships across the fleet.
5. The Stage-2 replacement decision, prepared as part of the next major release.

**Status of the contract source.** The organisation is evaluating Smithy as the modelling IDL; the
model is under active development and there is no committed rollout. The defects found by the live
runs are consistent with that work-in-progress status and are expected to be resolved as the model
matures. The C# requirement is narrower than that decision: a reliable,
validated OpenAPI document maintained as the contract artifact, from whichever IDL emits it —
Smithy and TypeSpec both output OpenAPI, and the C# toolchain consumes only the OpenAPI document.
The model gaps found by this spike (list envelope, nullability, unmodelled Auth) are input to that
evaluation. Stage 1 does not depend on the outcome: the drift monitor functions against an
imperfect specification and contributes to validating it. Stage 2 does depend on it.

**Reversal criteria.** Stop generation entirely if the OpenAPI contract artifact ceases to be
maintained. Do not proceed to Stage 2 if the shared model remains unvalidated or if only
runtime-coupled toolchains remain acceptable; in that case the existing hand-written types stay.

**Change triggers.** If the organisation mandates TypeSpec as source of truth, the conclusion is
unchanged (TypeSpec emits OpenAPI; the generator choice above still applies). If the team decides
to stop owning transport and adopt a whole generated client and runtime fleet-wide, reassess Kiota,
which is the stronger candidate for that scenario.

**Summary:** generate the wire data types with NSwag; use them first to monitor and prioritise
drift and to build new endpoints; align the existing public types at the next major version if the
contract source proves reliable; do not ship a generated client.

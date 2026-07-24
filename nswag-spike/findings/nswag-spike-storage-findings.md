# NSwag Spike #2 — Storage, and isolating manual-spec vs codegen issues

> Companion to `nswag-spike-findings.md`. Same tool/config (NSwag.ConsoleCore 14.7.1,
> System.Text.Json, injected HttpClient), different spec: `~/Downloads/api.json` =
> **Supabase Storage API**, OpenAPI 3.0.3, **machine-generated** (Fastify/JSON-schema; schema
> names `def-0`, `def-1`; `title: authSchema`). Reproduction in `_nswag-spike/`
> (`storage.pretty.json`, `StorageGeneratedClient.cs`).
>
> **Purpose:** the Auth spec is hand-written. By running an *auto-generated* spec through the
> same pipeline, we separate "problems caused by hand-authoring" from "problems inherent to
> spec→C# codegen / the tool."

## TL;DR

Running the two specs side by side sorts every problem into three buckets:

- **Manual-spec-only (Auth):** the two *ingest blockers* — NSwag's cast bug on reusable
  `responses` components, and a malformed `$ref`. **Storage hit neither** → these were artifacts
  of hand-authoring style, not of codegen.
- **Spec-shape, common to BOTH:** garbage operation names + invented request/response types +
  missing behavior. **Root cause identical in both specs: 0 `operationId`s and almost no named
  schemas.** This is the real, tool-independent ceiling.
- **Machine-spec-only (Storage):** *new* hard failures that make the output **not even compile** —
  `4XX`/`5XX` status ranges and `{*}` wildcard path params — plus massive surface bloat.

**Net:** neither spec makes *operation* codegen viable. The auto-generated spec is actually
**worse** than the hand-written one for codegen. The manual-vs-generated axis is not what decides
this — spec richness (operationIds + named schemas) is. Confirms the "**A for models, B for
operations**" boundary from both prior docs.

---

## 1. Spec profile — auto-generated, but thinner than Auth

| Metric | Auth (hand-written) | Storage (machine-generated) |
|---|---:|---:|
| Operations | ~60 | **110** |
| `operationId`s | **0** | **0** |
| Named component schemas | 15 | **3** (`def-0/1/2` = auth, error, vectorQuery) |
| Reusable `responses` components | 5 (→ Blocker A) | **0** |
| Inline `type: object` bodies | 103 | 94 |
| Wildcard `{*}` path params | none | **many** (`/object/{bucketName}/{*}`, `/s3/{Bucket}/{*}`, …) |
| `4XX`/`5XX` status ranges | no | **yes** |

Key point: being machine-generated did **not** give it operationIds or named request/response
schemas. It's a Fastify route dump — every route's body/response is an inline schema, so there's
nothing for a generator to name. It's *more* model-poor than the hand-written spec (3 vs 15).

Surface bloat: the spec exposes `s3/*`, `iceberg/*`, `vector/*`, `cdn/*`, `render/*`, plus
`HEAD`/`OPTIONS` twins of most routes — **110 operations** vs the handful the C# SDK actually
implements (bucket CRUD, object upload/download/list/move/copy, signed URLs). Generating the
whole spec produces a client that is mostly API the SDK has no intention of surfacing.

---

## 2. Ingest: Storage sailed past the manual-spec blockers

Auth needed a ~30-line preprocessor for two things; **Storage needed none of it:**

- **Blocker A (NSwag cast bug on `$ref` → `#/components/responses/*`):** Storage has **0**
  reusable responses → not triggered. This blocker was a property of how the Auth spec was
  *organized by hand*, not of codegen.
- **Blocker B (malformed `$ref`, response-in-schema-slot):** a hand-authoring mistake. Absent
  here.

So NSwag generated the Storage file in one shot (13,671 lines). **First isolation result: both
ingest blockers were manual-spec-specific.**

---

## 3. But the output does NOT compile — two machine-spec-specific bugs

Auth compiled after config fixes. Storage produced **214 compile errors** that are *inherent to
this spec's content*, not config:

### 3a. `4XX` / `5XX` status ranges → invalid C#
Fastify emits range status keys. NSwag renders them literally:

```csharp
else if (status_ == 4XX)   // CS1525 / CS1073: 4XX is not a number
{
    var objectResponse_ = await ReadObjectResponseAsync<Def1>(...);
```

### 3b. Wildcard `{*}` path param → invalid identifier
The catch-all route param becomes a parameter literally named `*`:

```csharp
Task ObjectGETAsync(string bucketName, string *, CancellationToken ...);  // CS1001
```

…and the same `*` leaks into the URL builder. Every wildcard route (a large fraction of the 110)
is broken this way.

Neither bug appeared in Auth. **Second isolation result: the machine-generated spec introduces
its own class of non-compiling output** (range statuses + wildcard routes) that the hand-written
one didn't.

---

## 4. Operations: identical failure mode in both (the tool-independent ceiling)

With **0 operationIds**, NSwag falls back to `{lastPathSegment}{METHOD}` + numeric
disambiguation — same as Auth, arguably worse due to HEAD/OPTIONS twins:

```
S3GETAsync / S3GET2Async / S3GET3Async / S3GET4Async
PublicGETAsync / PublicGET2Async / PublicGET3Async
AuthenticatedHEADAsync / AuthenticatedHEAD2Async / AuthenticatedHEAD3Async
ResumableOPTIONSAsync / ResumableOPTIONS2Async
```

(The `/vector/*` methods got clean names — `CreateIndexAsync`, `QueryVectorsAsync` — *only*
because those paths are RPC-style verbs, i.e. accidental, not from opIds.)

**Response typing is nearly absent:** of 110 methods, **81 return bare `Task`** (no typed body)
and only 29 return a `Task<...>`. The spec doesn't type its responses, so the generated client
gives callers nothing back for most calls.

**Third isolation result: the operation-layer garbage is identical across a hand-written and a
machine-generated spec. It is driven purely by missing operationIds + unnamed bodies — not by who
wrote the spec, and not by NSwag.**

---

## 5. Models: worse names than Auth, and the real value is unspillable anyway

The 3 named schemas → `Def0`, `Def1`, `Def2`. Inline objects → a swarm of positional names:
`Buckets`, `Buckets2`, `Buckets3`, `Created_at`, `Created_at2`, `Created_at3`, `Metadata4`,
`SortBy2`, `Id3`, `File_size_limit2`, … A couple got title-based names (`BucketSchema`,
`ObjectSchema`), but most are unusable.

Compare hand-written Storage models: `Bucket`, `FileObject`, `FileObjectV2` — clean, owned.

And per the original investigation (§3/§5): Storage's actual value is **`ProgressableStreamContent`,
`HttpClientProgress`, and TUS resumable upload** — none of which is expressible in the spec or
emittable by any generator. NSwag turned the resumable-upload routes into broken `Resumable*Async`
stubs (the very routes hit hardest by the `{*}` bug).

---

## 6. Conclusion — what "isolating manual-spec issues" actually showed

The manual-vs-generated distinction explains **only the ingest blockers**, and it cuts the
*opposite* way from the intuition that "a machine-generated spec will be cleaner":

| Bucket | Issues | Fix owner |
|---|---|---|
| **Manual-spec only** | NSwag responses-`$ref` cast bug; malformed `$ref` | spec authoring / preprocessor |
| **Common (spec-shape)** | garbage op names; invented `Body`/`Response`/`Def`/`Buckets2` types; untyped responses; no behavior | **upstream spec: add operationIds + named schemas** |
| **Machine-spec only** | `4XX`/`5XX` non-compile; `{*}` wildcard non-compile; surface bloat (s3/iceberg/vector, HEAD/OPTIONS) | spec dialect / generator config |

**The decisive variable is spec richness, not authorship.** Both specs have 0 operationIds and
near-zero named request/response schemas, so both produce unusable operation layers regardless of
tool. The auto-generated Storage spec is *worse* for codegen than the hand-written Auth spec:
fewer named schemas, non-compiling range/wildcard constructs, and 110 mostly-irrelevant routes.

Recommendation is unchanged and reinforced: **generate models only** (and even then, expect to
polish naming/nullability and fix the spec), **hand-write operations + transport on owned `Core`.**
For Storage specifically, the highest-value code (streaming/progress/TUS) is out of codegen's
reach entirely.

### Prerequisite spec fixes (help every language SDK, independent of tool)
1. Add `operationId` to all operations — kills `S3GET4Async` / `VerifyPOST2Async` naming.
2. Promote inline bodies/responses to named component schemas — kills `Def1` / `Buckets2` /
   `Response15`; enables typed returns.
3. Replace `4XX`/`5XX` range statuses with concrete codes (or accept per-tool patching).
4. Represent catch-all `{*}` routes with named path params (`{path}`) so params are valid C#.
5. Trim/segment the emitted surface to what each SDK ships (tags or a filtered spec).

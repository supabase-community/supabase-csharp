# spike-nswag.TestRun — manual run of the NSwag client vs local Supabase

Hand-written (ownable) console harness that drives the **NSwag-generated Storage client**
(`../nswag-spike`) against a **local Supabase platform**. Mirror of `../kiota-testrun` for a
like-for-like comparison.

## What it verifies

Same steps as the Kiota harness: HttpClient injection, ListBuckets / CreateBucket round-trip,
streaming upload (FileStream → `FileParameter`/`StreamContent`), ListObjects.

## Run

```bash
supabase start ; supabase status     # copy the API URL + service_role key
SUPABASE_URL=http://127.0.0.1:54321 SUPABASE_KEY=<service_role key> dotnet run
```

## Status — executed ✅ (2 passed / 2 failed — and the failures are the finding)

| Step | Result | Why |
|------|--------|-----|
| HttpClient injection | ✅ | client built over an external `HttpClient` |
| ListBuckets | ❌ | **model envelope bug** — API returns a bare array, model expects `{items:[]}` → NSwag **throws** `ApiException` (Kiota silently returned 0) |
| CreateBucket | ✅* | *only after working around the next bug |
| **UploadObject (streaming)** | ✅ | FileStream streamed via `StreamContent` — object confirmed on server |
| ListObjects | ❌ | NSwag serializes `sortBy:null` / `limit:0` → server 400s |

**Two NSwag bugs the run exposed (static analysis missed both):**

1. **NSwag emits every optional field at its default and serializes it** (no `WhenWritingNull`):
   `file_size_limit:0`, `limit:0`, `offset:0`, `sortBy:null`. The server rejects several —
   `file_size_limit:0` creates a bucket that **413s every upload**; `limit:0` → *"must be >= 1"*;
   `sortBy:null` → *"must be object"*. Optional value types are non-nullable and always sent.
   **Kiota omits unset fields → its calls just worked.** So NSwag's request DTOs are **not usable
   as-is** against Supabase without a serialization fix (`DefaultIgnoreCondition = WhenWritingNull`
   + nullable value types, or nullable annotations upstream).
2. **Shared list-envelope model bug** (same as Kiota) — but NSwag **throws** where Kiota silently
   returned empty. Loud-fail is arguably the safer behaviour.

See `../nswag-spike/evaluation-nswag.md` (Live run section) and the root `../codegen-comparison.md`.

> Isolated spike project: in `Supabase.sln`, **not referenced by `Supabase.csproj`**.

# spike-kiota.TestRun — manual run of the Kiota client vs local Supabase

Hand-written (ownable) console harness that drives the **Kiota-generated Storage client**
(`../kiota-spike`) against a **local Supabase platform**, to actually exercise the brief's
reachable deliverables rather than only reason about them.

## What it verifies

| Deliverable | How |
|---|---|
| **HttpClient injection** | client is built over an externally-created `HttpClient` (DI/`IHttpClientFactory` shape) carrying the `apikey` + `Authorization` headers |
| **Real round-trip** | `ListBuckets`, `CreateBucket` against the live Storage API |
| **Streaming upload** | `UploadObject` from a `FileStream` via Kiota `MultipartBody` — streamed, never read into a `byte[]` |
| confirm | `ListObjects` shows the uploaded object |

**Not covered (by design):**
- *Streaming response* — the Smithy Storage model has no object-download operation; that
  deliverable belongs to `Functions.Invoke` (`Task<Stream>`), which needs a deployed edge function.
- *Auth / PostgREST* — Auth isn't modelled upstream; PostgREST's value is the hand-written builder.

## Run it

```bash
# 1. start a local platform (from any Supabase project dir; needs Docker)
supabase start
supabase status          # note the API URL and the service_role key

# 2. run the harness (from this folder)
SUPABASE_URL=http://127.0.0.1:54321 \
SUPABASE_KEY=<service_role key from `supabase status`> \
dotnet run
```

Defaults (no env vars): `http://127.0.0.1:54321` and the well-known local **demo** service_role key.
If your CLI generated different keys, pass `SUPABASE_KEY` explicitly.

Expected output ends with `Result: N passed, 0 failed`; each step prints `[✓]`/`[✗]` with detail.
Exit code is non-zero if any step fails (CI-friendly).

## Status — executed against a local platform ✅ (with one real finding)

All 4 steps pass. Confirmed end-to-end against the live API:
- **HttpClient injection** and **streaming upload** genuinely work — the uploaded object
  (`hello.txt`, 61 bytes) is present on the server.

**Finding the run surfaced (static analysis missed it):** `ListBuckets`/`ListObjects` print `0`
even though the data exists. The real Storage API returns **bare JSON arrays**
(`GET /bucket → [ {...} ]`), but the Smithy model wraps them (`ListBucketsOutput { items: [...] }`),
so the generated client deserializes an array into an object → `.Items == null` → **false zero, no
error**. Tool-independent (NSwag has the same envelope) and affects every SDK — a model-vs-production
mismatch to raise on `supabase/sdk`. See `../kiota-spike/evaluation-kiota.md` §10.

> Isolated spike project: in `Supabase.sln` for convenience, **not referenced by `Supabase.csproj`**.

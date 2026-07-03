// Manual test-run for the Kiota-generated Supabase Storage client (SDK-1107).
//
// Verifies, against a LOCAL Supabase platform, the brief's reachable deliverables for Kiota:
//   • HttpClient injection  — we build the client over an externally-created HttpClient
//   • real round-trip       — ListBuckets / CreateBucket against the live Storage API
//   • streaming upload       — UploadObject from a FileStream via MultipartBody (no byte[] buffering)
//
// Not covered here (and why):
//   • streaming *response* — the Smithy Storage model has no object-download op; the streaming-
//     response deliverable lives in Functions.Invoke (needs a deployed edge function) — separate run.
//   • Auth / PostgREST     — Auth isn't modelled upstream; PostgREST value is the hand-written builder.
//
// Run:
//   supabase start                       # from any local Supabase project
//   supabase status                      # copy the service_role key + API URL
//   SUPABASE_URL=http://127.0.0.1:54321 SUPABASE_KEY=<service_role key> dotnet run
//
// Defaults below use the well-known local demo key; override via env for your instance.

using System.Text;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Supabase.Storage.Kiota;
using Supabase.Storage.Kiota.Models;

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "http://127.0.0.1:54321";
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY")
    ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";

Console.WriteLine($"Supabase URL : {supabaseUrl}");
Console.WriteLine($"Storage base : {supabaseUrl}/storage/v1");
Console.WriteLine($"Key          : {supabaseKey[..12]}…\n");

// ── HttpClient injection ────────────────────────────────────────────────────
// Externally-created HttpClient carrying the Supabase auth headers. Kiota uses THIS client
// (DI / IHttpClientFactory pattern) instead of creating its own.
var http = new HttpClient();
http.DefaultRequestHeaders.Add("apikey", supabaseKey);
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http)
{
    BaseUrl = $"{supabaseUrl}/storage/v1"
};
var client = new StorageApiClient(adapter);
Console.WriteLine("[✓] HttpClient injection — client built over an external HttpClient\n");

var bucketId = $"kiota-spike-{DateTime.UtcNow:yyyyMMddHHmmss}";
var pass = 0;
var fail = 0;

async Task Step(string name, Func<Task> action)
{
    try { await action(); Console.WriteLine($"[✓] {name}\n"); pass++; }
    catch (Exception ex) { Console.WriteLine($"[✗] {name}\n    {ex.GetType().Name}: {ex.Message}\n"); fail++; }
}

// ── round-trip: list buckets ────────────────────────────────────────────────
await Step("ListBuckets", async () =>
{
    // Envelope fix: the response is a bare array; the generated client now returns
    // List<Bucket> directly (previously an .Items envelope that never matched the wire
    // shape, so this call silently returned 0 — the false-zero failure).
    var buckets = await client.Bucket.GetAsync();
    Console.WriteLine($"    server returned {buckets?.Count ?? 0} bucket(s)");
});

// ── round-trip: create bucket ───────────────────────────────────────────────
await Step($"CreateBucket ({bucketId})", async () =>
{
    var resp = await client.Bucket.PostAsync(new CreateBucketRequestContent
    {
        Id = bucketId,
        Name = bucketId,
        Public = false
    });
    Console.WriteLine($"    created (response stream: {(resp is null ? "none" : "received")})");
    resp?.Dispose();
});

// ── streaming upload: FileStream → MultipartBody (no byte[] buffering) ───────
var objectName = "hello.txt";
await Step($"UploadObject (streaming FileStream → {bucketId}/{objectName})", async () =>
{
    // a real on-disk file, streamed — never read into a byte[]
    var tmp = Path.GetTempFileName();
    await File.WriteAllTextAsync(tmp, $"streamed from Kiota test-run at {DateTime.UtcNow:O}\n");
    var sizeKb = new FileInfo(tmp).Length / 1024.0;

    await using var fileStream = File.OpenRead(tmp); // <-- streamed, not buffered
    var multipart = new MultipartBody { RequestAdapter = adapter };
    multipart.AddOrReplacePart("cacheControl", "text/plain", "3600");
    multipart.AddOrReplacePart("metadata", "application/json", "{}");
    multipart.AddOrReplacePart("file", "text/plain", fileStream, objectName);

    var resp = await client.Object[bucketId][objectName].PostAsync(multipart);
    Console.WriteLine($"    uploaded {sizeKb:0.00} KB via StreamContent; key = {resp?.Key ?? "(none)"}");
    File.Delete(tmp);
});

// ── confirm the object landed ───────────────────────────────────────────────
await Step("ListObjects (confirm upload)", async () =>
{
    var listed = await client.Object.List[bucketId].PostAsync(new ListObjectsRequestContent { Prefix = "" });
    Console.WriteLine($"    bucket now holds {listed?.Count ?? 0} object(s)");
});

Console.WriteLine(new string('─', 60));
Console.WriteLine($"Result: {pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;

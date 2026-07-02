// Manual test-run for the NSwag-generated Supabase Storage client (SDK-1107).
// Mirror of ../kiota-testrun — same steps, same local platform — for a like-for-like comparison.
//
// Verifies against a LOCAL Supabase platform:
//   • HttpClient injection  — the NSwag client takes an external HttpClient in its constructor
//   • real round-trip       — ListBuckets / CreateBucket against the live Storage API
//   • streaming upload       — UploadObject from a FileStream via FileParameter/StreamContent
//
// Run:
//   supabase start ; supabase status              # copy service_role key + API URL
//   SUPABASE_URL=http://127.0.0.1:54321 SUPABASE_KEY=<service_role key> dotnet run

using Supabase.Storage.Gen;

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "http://127.0.0.1:54321";
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY")
    ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";

Console.WriteLine($"Supabase URL : {supabaseUrl}");
Console.WriteLine($"Storage base : {supabaseUrl}/storage/v1");
Console.WriteLine($"Key          : {supabaseKey[..12]}…\n");

// ── HttpClient injection ────────────────────────────────────────────────────
// NSwag client ctor takes an external HttpClient. Base address carries /storage/v1/ (the generated
// client builds relative URIs); auth headers go on the client (DI / IHttpClientFactory shape).
var http = new HttpClient { BaseAddress = new Uri($"{supabaseUrl}/storage/v1/") };
http.DefaultRequestHeaders.Add("apikey", supabaseKey);
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

var client = new StorageClient(http);
Console.WriteLine("[✓] HttpClient injection — client built over an external HttpClient\n");

var bucketId = $"nswag-spike-{DateTime.UtcNow:yyyyMMddHHmmss}";
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
    var buckets = await client.ListBucketsAsync();
    Console.WriteLine($"    server returned {buckets.Items?.Count ?? 0} bucket(s)");
});

// ── round-trip: create bucket ───────────────────────────────────────────────
await Step($"CreateBucket ({bucketId})", async () =>
{
    // NSwag GOTCHA: File_size_limit is a non-nullable `double` that defaults to 0 and IS serialized,
    // so `file_size_limit: 0` creates a bucket that rejects EVERY upload (413). We must set it
    // explicitly. (Kiota models it as `double?` and omits it → unlimited.) See evaluation-nswag §3a.
    await client.CreateBucketAsync(new CreateBucketRequestContent
    {
        Id = bucketId, Name = bucketId, Public = false, File_size_limit = 52428800 /* 50 MB */
    });
    Console.WriteLine("    created (had to set File_size_limit — NSwag defaults it to 0)");
});

// ── streaming upload: FileStream → FileParameter/StreamContent (no byte[] buffering) ──
var objectName = "hello.txt";
await Step($"UploadObject (streaming FileStream → {bucketId}/{objectName})", async () =>
{
    var tmp = Path.GetTempFileName();
    await File.WriteAllTextAsync(tmp, $"streamed from NSwag test-run at {DateTime.UtcNow:O}\n");
    var sizeKb = new FileInfo(tmp).Length / 1024.0;

    await using var fileStream = File.OpenRead(tmp); // <-- streamed, not buffered
    var file = new FileParameter(fileStream, objectName, "text/plain");

    var resp = await client.UploadObjectAsync(bucketId, objectName, x_upsert: "true",
        file: file, cacheControl: "3600", metadata: new { });
    Console.WriteLine($"    uploaded {sizeKb:0.00} KB via StreamContent; key = {resp?.Key ?? "(none)"}");
    File.Delete(tmp);
});

// ── confirm the object landed ───────────────────────────────────────────────
await Step("ListObjects (confirm upload)", async () =>
{
    // NSwag serializes ALL optional fields at their default (no WhenWritingNull), so even a minimal
    // call sends limit:0 / offset:0 / sortBy:null. The server rejects each in turn:
    //   limit:0   → "body/limit must be >= 1"     (so we set Limit=100)
    //   sortBy:null → "body/sortBy must be object"  (can't be fixed from here — it's still serialized)
    // Kiota omits unset fields, so its equivalent call just works. This step therefore still fails,
    // demonstrating that NSwag's request DTOs don't work against the server with default serialization.
    var listed = await client.ListObjectsAsync(new ListObjectsRequestContent { Prefix = "", Limit = 100 }, bucketId);
    Console.WriteLine($"    bucket now holds {listed.Items?.Count ?? 0} object(s)");
});

Console.WriteLine(new string('─', 60));
Console.WriteLine($"Result: {pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;

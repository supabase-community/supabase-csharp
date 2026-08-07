# Supabase.Storage

[![Build and Test](https://github.com/supabase-community/storage-csharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/supabase-community/storage-csharp/acionts/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Supabase.Storage")](https://www.nuget.org/packages/Supabase.Storage/)

---

Integrate your [Supabase](https://supabase.io) projects with C#.

## [Notice]: v2.0.0 renames this package from `storage-csharp` to `Supabase.Storage`. The depreciation notice has been set in NuGet. The API remains the same.

## Examples (using supabase-csharp)

```c#
public async void Main()
{
  // Make sure you set these (or similar)
  var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
  var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

  await Supabase.Client.InitializeAsync(url, key);

  // The Supabase Instance can be accessed at any time using:
  //  Supabase.Client.Instance {.Realtime|.Auth|etc.}
  // For ease of readability we'll use this:
  var instance = Supabase.Client.Instance;

  // Interact with Supabase Storage
  var storage = Supabase.Client.Instance.Storage
  await storage.CreateBucket("testing")

  var bucket = storage.From("testing");

  var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
  var imagePath = Path.Combine(basePath, "Assets", "supabase-csharp.png");

  await bucket.Upload(imagePath, "supabase-csharp.png");

  // If bucket is public, get url
  bucket.GetPublicUrl("supabase-csharp.png");

  // If bucket is private, generate url
  await bucket.CreateSignedUrl("supabase-csharp.png", 3600));

  // Download it!
  await bucket.Download("supabase-csharp.png", Path.Combine(basePath, "testing-download.png"));
}
```

## Observability (OpenTelemetry)

The client emits traces and metrics through `System.Diagnostics`, so you can wire them into
OpenTelemetry (or any `ActivityListener`/`MeterListener`) without the client taking a dependency
on the OpenTelemetry packages. Emission is zero-cost while nothing is listening, so it is always
on and stays silent until you subscribe.

Register the client's `ActivitySource` and `Meter` by name. Use the `StorageDiagnostics.SourceName`
constant rather than hardcoding the string, so a typo becomes a compile error instead of a silent
no-op:

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Supabase.Storage;

// Requires the OpenTelemetry.Extensions.Hosting and an exporter package (e.g. OTLP) in your app.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(StorageDiagnostics.SourceName)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(StorageDiagnostics.SourceName)
        .AddOtlpExporter());
```

Once subscribed you get:

- A client span per request, named `{METHOD} {path}` and following OpenTelemetry HTTP conventions
  (method, status code, and a sanitized URL). The query string is **never** recorded — Storage
  signed URLs carry a `token` there. Upload and download spans additionally carry a
  `storage.transfer.direction` tag (`upload` / `download`). The resumable (TUS) upload is reported
  as a single operation span covering the whole transfer, rather than one span per underlying chunk
  request.
- `supabase.storage.http.request.duration` (seconds) for control-plane requests.
- `supabase.storage.transfer.duration` (seconds) and `supabase.storage.transfer.size` (bytes) for
  uploads and downloads, tagged by direction — because a duration alone does not describe a file
  transfer.

If you are not using the OpenTelemetry SDK, a raw listener works too:

```csharp
using System.Diagnostics;
using Supabase.Storage;

using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == StorageDiagnostics.SourceName,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => Console.WriteLine($"{activity.OperationName} {activity.Duration.TotalMilliseconds}ms {activity.Status}")
};
ActivitySource.AddActivityListener(listener);
```

## Package made possible through the efforts of:

Join the ranks! See a problem? Help fix it!

<a href="https://github.com/supabase-community/storage-csharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=supabase-community/storage-csharp" />
</a>

Made with [contrib.rocks](https://contrib.rocks/preview?repo=supabase-community%storage-csharp).

## Contributing

We are more than happy to have contributions! Please submit a PR.

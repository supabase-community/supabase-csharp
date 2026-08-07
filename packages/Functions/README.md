# Supabase.Functions

[![Build and Test](https://github.com/supabase-community/functions-csharp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/supabase-community/functions-csharp/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Supabase.Functions)](https://www.nuget.com/packages/Supabase.Functions/)

---

## [Notice]: v2.0.0 renames this package from `functions-csharp` to `Supabase.Functions`. The depreciation notice has been set in NuGet. The API remains the same.

C# Client library to interact with Supabase Functions.

## Observability (OpenTelemetry)

The client emits traces and metrics through `System.Diagnostics`, so you can wire them into
OpenTelemetry (or any `ActivityListener`/`MeterListener`) without the client taking a dependency
on the OpenTelemetry packages. Emission is zero-cost while nothing is listening, so it is always
on and stays silent until you subscribe.

Register the client's `ActivitySource` and `Meter` by name. Use the `FunctionsDiagnostics.SourceName`
constant rather than hardcoding the string, so a typo becomes a compile error instead of a silent
no-op:

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Supabase.Functions;

// Requires the OpenTelemetry.Extensions.Hosting and an exporter package (e.g. OTLP) in your app.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(FunctionsDiagnostics.SourceName)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(FunctionsDiagnostics.SourceName)
        .AddOtlpExporter());
```

Once subscribed you get:

- A client span per invocation, named `{METHOD} {path}` and following OpenTelemetry HTTP conventions
  (method, status code, and a sanitized URL — the query string is never recorded, and neither is the
  request body). A `faas.invoked_name` tag carries the invoked function name. A relay error is
  reported as a failed span even when the HTTP status is a success.
- A `supabase.functions.invoke.duration` histogram (seconds), tagged with method, host, path,
  function name, and status code.

If you are not using the OpenTelemetry SDK, a raw listener works too:

```csharp
using System.Diagnostics;
using Supabase.Functions;

using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == FunctionsDiagnostics.SourceName,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => Console.WriteLine($"{activity.OperationName} {activity.Duration.TotalMilliseconds}ms {activity.Status}")
};
ActivitySource.AddActivityListener(listener);
```

## Package made possible through the efforts of:

Join the ranks! See a problem? Help fix it!

<a href="https://github.com/supabase-community/functions-csharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=supabase-community/functions-csharp" />
</a>

<small>Made with [contrib.rocks](https://contrib.rocks).</small>

## Contributing

We are more than happy to have contributions! Please submit a PR.

### Testing

To run the tests locally you must have docker and docker-compose installed. Then in the root of the repository run:

- `docker-compose up -d`
- `dotnet test`

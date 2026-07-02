# Kiota codegen spike (C# SDK · SDK-1107)

Comparison point for the NSwag spike (`../nswag-spike`). Same input — the committed
Smithy→OpenAPI artifacts from `supabase/sdk#51` — run through **Microsoft Kiota** instead of NSwag.

> ⚠️ **Spike, not shippable code.** Everything under `generated/` is machine-emitted and
> **not owned/maintainable**. Standalone project (`spike-kiota.csproj`, separate assembly) added to
> `Supabase.sln` for visibility only; **not referenced by `Supabase.csproj`**, analyzers disabled —
> does **not** affect the product's build/analyzers/quality metrics. Do not reference from the SDK.

## Generated with

```bash
dotnet tool install --global Microsoft.OpenApi.Kiota   # Kiota 1.32.4
kiota generate -l CSharp -d generated/StorageService.ready.json \
  -c StorageApiClient -n Supabase.Storage.Kiota -o generated/Storage --clean-output
# (Functions / Database identical)
```

## What it produced

| Service | Files | Shape |
|---------|------:|-------|
| Storage | 51 | request-builder tree (`client.Bucket["id"].DeleteAsync()`) + models |
| Functions | 5 | " |
| Database | 5 | " |

Runtime dependency: **`Microsoft.Kiota.Bundle`** → pulls 8 packages (`Kiota.Abstractions`,
`Http.HttpClientLibrary`, `Serialization.Json/Form/Text/Multipart`, `Std.UriTemplate`).

## Why it exists / conclusion

Per-toolchain detail: **[`evaluation-kiota.md`](evaluation-kiota.md)**. Cross-toolchain
comparison + overall recommendation: root **[`codegen-comparison.md`](../codegen-comparison.md)**.
In short:

- **Kiota wins** where NSwag lost — **AOT/trimming** (explicit `IParsable`, zero reflection),
  **streaming responses** (native `Task<Stream>`), and **model naming** (`FileSizeLimit`).
- **Kiota loses** where this SDK cares most — **ownership**: 8 runtime packages, a request-builder
  tree, and models coupled to the Kiota runtime (`IParsable`), so they **can't be lifted out
  standalone**. That defeats the "models-only, plain POCOs" strategy NSwag supports.

**Recommendation stands: NSwag is the better fit for supabase-csharp** (plain, ownable, zero-dep;
usable models-only). Kiota is the choice only if adopting a *whole* generated client + its runtime
fleet-wide. NSwag's one weakness (AOT) is closable in-place via a source-gen `JsonSerializerContext`.

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

## Package made possible through the efforts of:

Join the ranks! See a problem? Help fix it!

<a href="https://github.com/supabase-community/storage-csharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=supabase-community/storage-csharp" />
</a>

Made with [contrib.rocks](https://contrib.rocks/preview?repo=supabase-community%storage-csharp).

## Contributing

We are more than happy to have contributions! Please submit a PR.

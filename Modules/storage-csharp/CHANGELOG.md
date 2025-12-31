# Changelog

## [2.4.1](https://github.com/supabase-community/storage-csharp/compare/v2.4.0...v2.4.1) (2025-10-18)


### Bug Fixes

* implement in-memory caching for resumable uploads, optimize erro… ([#35](https://github.com/supabase-community/storage-csharp/issues/35)) ([7a78ea7](https://github.com/supabase-community/storage-csharp/commit/7a78ea759024832e28a538e0d8eb195b5eee7226))

## [2.4.0](https://github.com/supabase-community/storage-csharp/compare/v2.3.0...v2.4.0) (2025-09-09)


### Features

* implement cancelation token on some of upload method ([#30](https://github.com/supabase-community/storage-csharp/issues/30)) ([3befec0](https://github.com/supabase-community/storage-csharp/commit/3befec0181de15459fa427e32bebc4b92d82c161))

## [2.3.0](https://github.com/supabase-community/storage-csharp/compare/v2.2.0...v2.3.0) (2025-09-09)


### Features

* resumable upload ([#29](https://github.com/supabase-community/storage-csharp/issues/29)) ([e9a94f9](https://github.com/supabase-community/storage-csharp/commit/e9a94f98e9d18b9c9448e77537f974e4c56e3134))

## [2.2.0](https://github.com/supabase-community/storage-csharp/compare/v2.1.0...v2.2.0) (2025-06-06)


### Miscellaneous Chores

* update readme title ([83dcdfe](https://github.com/supabase-community/storage-csharp/commit/83dcdfe6c3f153b63b1c493cfaf1d53b3ae9f737))

## [2.1.0](https://github.com/supabase-community/storage-csharp/compare/v2.0.2...v2.1.0) (2025-05-25)


### Miscellaneous Chores

* release 2.1.0 ([d037a54](https://github.com/supabase-community/storage-csharp/commit/d037a54a55dae8f4d7f13d57bcfb0d6c166472a9))

## 2.0.2 - 06-29-2024

- Removes unused testing dependencies from `Storage.csproj` that may have caused build errors for projects.

## 2.0.1 - 05-16-2024

- Re: [#15](https://github.com/supabase-community/storage-csharp/issues/15)
  and [#16](https://github.com/supabase-community/storage-csharp/pull/16)
  Fix CreateSignedUrl with TransformOptions. Thanks [@alustrement-bob](https://github.com/alustrement-bob)!

## 2.0.0 - 04-21-2024

- Re: [#135](https://github.com/supabase-community/supabase-csharp/issues/135) Update nuget package
  name `storage-csharp` to `Supabase.Storage`

## 1.4.0 - 08-26-2023

- Fixes [#11](https://github.com/supabase-community/storage-csharp/issues/11) - Which implements
  missing `SupabaseStorageException` on failure status codes for `Upload`, `Download`, `Move`, `CreateSignedUrl`
  and `CreateSignedUrls`.

## 1.3.2 - 06-10-2023

- Uses new `Supabase.Core` assembly name.
- Renames output assembly to `Supabase.Storage`.

## 1.3.0 - 05-06-2023

- Re: [supabase-community/gotrue-csharp#57](https://github.com/supabase-community/gotrue-csharp/pull/57) - cleaner
  exception handling + expanded tests.
- Re: [#9](https://github.com/supabase-community/storage-csharp/issues/9) - `FileObject` supports the return of
  folders (use `IsFolder`) property to distinguish
- Re: [#8](https://github.com/supabase-community/storage-csharp/issues/8) - Fixes Socket Starvation issue by using
  static `HttpClient`s

## 1.2.10 - 04-17-2023

- Re: [#7](https://github.com/supabase-community/storage-csharp/issues/7) Implements a `DownloadPublicFile` method.

## 1.2.9 - 04-12-2023

Implements storage features from LW7:

- feat: custom file size limit and mime types at bucket
  level [supabase/storage-js#151](https://github.com/supabase/storage-js/pull/151) file size and mime type limits per
  bucket
- feat: quality option, image transformation [supabase/storage-js#145](https://github.com/supabase/storage-js/pull/152)
  quality option for image transformations
- feat: format option for webp support [supabase/storage-js#142](https://github.com/supabase/storage-js/pull/142) format
  option for image transformation

## 1.2.8 - 03-14-2023

- [Merge #5](https://github.com/supabase-community/storage-csharp/pull/5) Added search string as an optional search
  parameter. Thanks [@ElectroKnight22](https://github.com/ElectroKnight22)!

## 1.2.7 - 03-02-2023

- Fix incorrect namespacing for Supabase.Storage.ClientOptions.

## 1.2.6 - 03-02-2023

- Re: [#4](https://github.com/supabase-community/storage-csharp/issues/4) Implementation for `ClientOptions` which
  supports specifying Upload, Download, and Request timeouts.

## 1.2.5 - 02-28-2023

- Provides fix
  for [supabase-community/supabase-csharp#54](https://github.com/supabase-community/supabase-csharp/issues/54) - Dynamic
  headers were always being overwritten by initialized headers, so the storage client would not receive user's access
  token as expected.
- Provides fix for upload progress not reporting
  in [supabase-community/storage-csharp#3](https://github.com/supabase-community/storage-csharp/issues/3)

## 1.2.4 - 02-26-2023

- `UploadOrUpdate` now appropriately throws request exception if server returns a bad status code.

## 1.2.3 - 11-12-2022

- Use `supabase-core` and implement `IGettableHeaders` on `Client`
- `Client` no longer has `headers` as a required parameter.

## 1.2.2 - 11-10-2022

- Clarifies `IStorageClient` as implementing `IStorageBucket`

## 1.2.1 - 11-10-2022

- Expose `StorageBucketApi.Headers` as a public property.

## 1.2.0 - 11-4-2022

- [#2](https://github.com/supabase-community/storage-csharp/issues/2) Restructure Library to support Dependency
  Injection (DI)
- Enable nullability in the project and make use of nullable reference types.

## 1.1.1 - 07-17-2022

- Fix missing API change on `Update` method of `StorageFileApi`

## 1.1.0 - 07-17-2022

- API Change [Breaking/Minor] Library no longer uses `WebClient` and instead leverages `HttpClient`. Progress events
  on `Upload` and `Download` are now handled with `EventHandler<float>` instead of `WebClient` EventHandlers.

## 1.0.2 - 02-27-2022

- Add `CreatedSignedUrls` method.

## 1.0.1 - 12-9-2021

- Add missing support for `X-Client-Info`

## 1.0.0 - 12-9-2021

- Initial release of separated storage client

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BirdMessenger;
using BirdMessenger.Collections;
using BirdMessenger.Delegates;
using BirdMessenger.Infrastructure;
using Newtonsoft.Json;
using Supabase.Storage.Exceptions;

namespace Supabase.Storage.Extensions
{
    /// <summary>
    /// Adapted from: https://gist.github.com/dalexsoto/9fd3c5bdbe9f61a717d47c5843384d11
    /// </summary>
    internal static class HttpClientProgress
    {
        public static async Task<MemoryStream> DownloadDataAsync(
            this HttpClient client,
            Uri uri,
            Dictionary<string, string>? headers = null,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var destination = new MemoryStream();
            var message = new HttpRequestMessage(HttpMethod.Get, uri);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }

            using (
                var response = await client.SendAsync(
                    message,
                    HttpCompletionOption.ResponseHeadersRead
                )
            )
            {
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    var e = new SupabaseStorageException(errorResponse?.Message ?? content)
                    {
                        Content = content,
                        Response = response,
                        StatusCode = errorResponse?.StatusCode ?? (int)response.StatusCode,
                    };

                    e.AddReason();
                    throw e;
                }

                var contentLength = response.Content.Headers.ContentLength;
                using (var download = await response.Content.ReadAsStreamAsync())
                {
                    // no progress... no contentLength... very sad
                    if (progress is null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return destination;
                    }

                    // Such progress and contentLength much reporting Wow!
                    var progressWrapper = new Progress<long>(totalBytes =>
                        progress.Report(GetProgressPercentage(totalBytes, contentLength.Value))
                    );
                    await download.CopyToAsync(
                        destination,
                        81920,
                        progressWrapper,
                        cancellationToken
                    );
                }
            }

            float GetProgressPercentage(float totalBytes, float currentBytes) =>
                (totalBytes / currentBytes) * 100f;

            return destination;
        }

        static async Task CopyToAsync(
            this Stream source,
            Stream destination,
            int bufferSize,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;

            while (
                (
                    bytesRead = await source
                        .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false)
                ) != 0
            )
            {
                await destination
                    .WriteAsync(buffer, 0, bytesRead, cancellationToken)
                    .ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }

        public static Task<HttpResponseMessage> UploadFileAsync(
            this HttpClient client,
            Uri uri,
            string filePath,
            Dictionary<string, string>? headers = null,
            Progress<float>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var fileStream = new FileStream(filePath, mode: FileMode.Open, FileAccess.Read);
            return UploadAsync(client, uri, fileStream, headers, progress, cancellationToken);
        }

        public static Task<HttpResponseMessage> UploadBytesAsync(
            this HttpClient client,
            Uri uri,
            byte[] data,
            Dictionary<string, string>? headers = null,
            Progress<float>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var stream = new MemoryStream(data);
            return UploadAsync(client, uri, stream, headers, progress, cancellationToken);
        }

        public static async Task<HttpResponseMessage> UploadAsync(
            this HttpClient client,
            Uri uri,
            Stream stream,
            Dictionary<string, string>? headers = null,
            Progress<float>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var content = new ProgressableStreamContent(stream, 4096, progress);

            if (headers != null)
            {
                client.DefaultRequestHeaders.Clear();

                foreach (var header in headers)
                {
                    if (header.Key.Contains("content"))
                        content.Headers.Add(header.Key, header.Value);
                    else
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var response = await client.PostAsync(uri, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var httpContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(httpContent);
                var e = new SupabaseStorageException(errorResponse?.Message ?? httpContent)
                {
                    Content = httpContent,
                    Response = response,
                    StatusCode = errorResponse?.StatusCode ?? (int)response.StatusCode,
                };

                e.AddReason();
                throw e;
            }

            return response;
        }

        public static Task<HttpResponseMessage> UploadOrContinueFileAsync(
            this HttpClient client,
            Uri uri,
            string filePath,
            MetadataCollection metadata,
            Dictionary<string, string>? headers = null,
            Progress<float>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var fileStream = new FileStream(filePath, mode: FileMode.Open, FileAccess.Read);
            return ResumableUploadAsync(
                client,
                uri,
                fileStream,
                metadata,
                headers,
                progress,
                cancellationToken
            );
        }

        public static Task<HttpResponseMessage> UploadOrContinueByteAsync(
            this HttpClient client,
            Uri uri,
            byte[] data,
            MetadataCollection metadata,
            Dictionary<string, string>? headers = null,
            Progress<float>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var stream = new MemoryStream(data);
            return ResumableUploadAsync(
                client,
                uri,
                stream,
                metadata,
                headers,
                progress,
                cancellationToken
            );
        }

        private static async Task<HttpResponseMessage> ResumableUploadAsync(
            this HttpClient client,
            Uri uri,
            Stream fileStream,
            MetadataCollection metadata,
            Dictionary<string, string>? headers = null,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            if (fileStream.Position != 0 && fileStream.CanSeek)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
            }

            if (headers != null)
            {
                client.DefaultRequestHeaders.Clear();
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var cacheKey =
                $"{metadata["bucketName"]}/{metadata["objectName"]}/{metadata["contentType"]}";

            UploadMemoryCache.TryGet(cacheKey, out var upload);
            Uri? fileLocation = null;
            if (upload == null)
            {
                var createOption = new TusCreateRequestOption()
                {
                    Endpoint = uri,
                    Metadata = metadata,
                    UploadLength = fileStream.Length,
                };

                try
                {
                    var responseCreate = await client.TusCreateAsync(
                        createOption,
                        cancellationToken
                    );

                    fileLocation = responseCreate.FileLocation;
                    UploadMemoryCache.Set(cacheKey, fileLocation.ToString());
                }
                catch (TusException error)
                {
                    throw await HandleResponseError(error);
                }
            }

            if (upload != null)
                fileLocation = new Uri(upload);

            var patchOption = new TusPatchRequestOption
            {
                FileLocation = fileLocation,
                Stream = fileStream,
                UploadBufferSize = 6 * 1024 * 1024,
                UploadType = UploadType.Chunk,
                OnProgressAsync = x => ReportProgressAsync(progress, x),
                OnCompletedAsync = _ =>
                {
                    UploadMemoryCache.Remove(cacheKey);
                    return Task.CompletedTask;
                },
                OnFailedAsync = _ => Task.CompletedTask,
            };

            var responsePatch = await client.TusPatchAsync(patchOption, cancellationToken);

            if (responsePatch.OriginResponseMessage.IsSuccessStatusCode)
                return responsePatch.OriginResponseMessage;

            throw await HandleResponseError(responsePatch.OriginResponseMessage);
        }

        private static Task ReportProgressAsync(
            IProgress<float>? progress,
            UploadProgressEvent progressInfo
        )
        {
            if (progress == null)
                return Task.CompletedTask;

            var uploadedProgress = (float)progressInfo.UploadedSize / progressInfo.TotalSize * 100f;
            progress.Report(uploadedProgress);

            return Task.CompletedTask;
        }

        private static async Task<SupabaseStorageException> HandleResponseError(
            HttpResponseMessage response
        )
        {
            var httpContent = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(httpContent);
            var error = new SupabaseStorageException(errorResponse?.Message ?? httpContent)
            {
                Content = httpContent,
                Response = response,
                StatusCode = errorResponse?.StatusCode ?? (int)response.StatusCode,
            };
            error.AddReason();

            return error;
        }

        private static async Task<SupabaseStorageException> HandleResponseError(
            TusException response
        )
        {
            var httpContent = await response.OriginHttpResponse.Content.ReadAsStringAsync();
            var error = new SupabaseStorageException(httpContent)
            {
                Content = httpContent,
                Response = response.OriginHttpResponse,
                StatusCode = (int)response.OriginHttpResponse.StatusCode,
            };
            error.AddReason();

            return error;
        }
    }
}

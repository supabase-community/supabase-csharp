using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class StorageFileApi
    {
        protected string Url { get; set; }
        protected Dictionary<string, string> Headers { get; set; }
        protected string BucketId { get; set; }

        public StorageFileApi(string url, Dictionary<string, string> headers = null, string bucketId = null)
        {
            Url = url;
            BucketId = bucketId;

            if (headers == null)
            {
                Headers = new Dictionary<string, string>();
            }
            else
            {
                Headers = headers;
            }
        }

        /// <summary>
        /// Create signed url to download file without requiring permissions. This URL can be valid for a set number of seconds.
        /// </summary>
        /// <param name="path">The file path to be downloaded, including the current file name. For example `folder/image.png`.</param>
        /// <param name="expiresIn">The number of seconds until the signed URL expires. For example, `60` for a URL which is valid for one minute.</param>
        /// <returns></returns>
        public async Task<string> CreateSignedUrl(string path, int expiresIn)
        {
            var body = new Dictionary<string, object> { { "expiresIn", expiresIn } };
            var response = await Helpers.MakeRequest<CreateSignedUrlResponse>(HttpMethod.Post, $"{Url}/object/sign/{GetFinalPath(path)}", body, Headers);

            return $"{Url}{response.SignedUrl}";
        }

        /// <summary>
        /// Retrieve URLs for assets in public buckets
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetPublicUrl(string path) => $"{Url}/object/public/{GetFinalPath(path)}";

        /// <summary>
        /// Lists all the files within a bucket.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<List<FileObject>> List(string path = "", SearchOptions options = null)
        {
            if (options == null)
            {
                options = new SearchOptions();
            }

            var json = JsonConvert.SerializeObject(options);
            var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            body.Add("prefix", string.IsNullOrEmpty(path) ? "" : path);

            var response = await Helpers.MakeRequest<List<FileObject>>(HttpMethod.Post, $"{Url}/object/list/{BucketId}", body, Headers);

            return response;
        }

        /// <summary>
        /// Uploads a file to an existing bucket.
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<string> Upload(string localFilePath, string supabasePath, FileOptions options = null, UploadProgressChangedEventHandler onProgress = null, bool inferContentType = true)
        {
            if (options == null)
            {
                options = new FileOptions();
            }

            if (inferContentType)
            {
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(localFilePath);
            }

            var result = await UploadOrUpdate(localFilePath, supabasePath, options, onProgress);
            return result;
        }

        /// <summary>
        /// Uploads a byte array to an existing bucket.
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<string> Upload(byte[] data, string supabasePath, FileOptions options = null, UploadProgressChangedEventHandler onProgress = null, bool inferContentType = true)
        {
            if (options == null)
            {
                options = new FileOptions();
            }

            if (inferContentType)
            {
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(supabasePath);
            }

            var result = await UploadOrUpdate(data, supabasePath, options, onProgress);
            return result;
        }

        /// <summary>
        /// Replaces an existing file at the specified path with a new one.
        /// </summary>
        /// <param name="localFilePath">File source path.</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options">HTTP headers.</param>
        /// <returns></returns>
        public async Task<string> Update(string localFilePath, string supabasePath, FileOptions options = null, UploadProgressChangedEventHandler onProgress = null)
        {
            if (options == null)
            {
                options = new FileOptions();
            }

            var result = await UploadOrUpdate(localFilePath, supabasePath, options);
            return result;
        }

        /// <summary>
        /// Replaces an existing file at the specified path with a new one.
        /// </summary>
        /// <param name="localFilePath">File source path.</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options">HTTP headers.</param>
        /// <returns></returns>
        public async Task<string> Update(byte[] data, string supabasePath, FileOptions options = null, UploadProgressChangedEventHandler onProgress = null)
        {
            if (options == null)
            {
                options = new FileOptions();
            }

            var result = await UploadOrUpdate(data, supabasePath, options);
            return result;
        }

        /// <summary>
        /// Moves an existing file, optionally renaming it at the same time.
        /// </summary>
        /// <param name="fromPath">The original file path, including the current file name. For example `folder/image.png`.</param>
        /// <param name="toPath">The new file path, including the new file name. For example `folder/image-copy.png`.</param>
        /// <returns></returns>
        public async Task<bool> Move(string fromPath, string toPath)
        {
            try
            {
                var body = new Dictionary<string, string> { { "bucketId", BucketId }, { "sourceKey", fromPath }, { "destinationKey", toPath } };
                await Helpers.MakeRequest<GenericResponse>(HttpMethod.Post, $"{Url}/object/move", body, Headers);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Downloads a file and saves it to a local path.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="localPath"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> Download(string supabasePath, string localPath, DownloadProgressChangedEventHandler onProgress = null)
        {
            var tsc = new TaskCompletionSource<string>();

            try
            {
                WebClient client = new WebClient();
                Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

                foreach (var header in Headers)
                    client.Headers.Add(header.Key, header.Value);

                if (onProgress != null)
                    client.DownloadProgressChanged += onProgress;


                client.DownloadFileCompleted += (sender, args) => tsc.SetResult(localPath);

                client.DownloadFileAsync(uri, localPath);
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }

            return tsc.Task;
        }

        /// <summary>
        /// Downloads a byte array to be used programmatically.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<byte[]> Download(string supabasePath, DownloadProgressChangedEventHandler onProgress = null)
        {
            var tsc = new TaskCompletionSource<byte[]>();

            try
            {
                WebClient client = new WebClient();
                Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

                foreach (var header in Headers)
                    client.Headers.Add(header.Key, header.Value);

                if (onProgress != null)
                    client.DownloadProgressChanged += onProgress;


                client.DownloadDataCompleted += (sender, args) => tsc.SetResult(args.Result);

                client.DownloadDataAsync(uri);
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }

            return tsc.Task;
        }

        /// <summary>
        /// Deletes files within the same bucket
        /// </summary>
        /// <param name="paths">An array of files to be deletes, including the path and file name. For example [`folder/image.png`].</param>
        /// <returns></returns>
        public async Task<List<FileObject>> Remove(List<string> paths)
        {
            var data = new Dictionary<string, object> { { "prefixes", paths } };
            var response = await Helpers.MakeRequest<List<FileObject>>(HttpMethod.Delete, $"{Url}/object/{BucketId}", data, Headers);

            return response;
        }

        private Task<string> UploadOrUpdate(string localPath, string supabasePath, FileOptions options, UploadProgressChangedEventHandler onProgress = null)
        {
            var tsc = new TaskCompletionSource<string>();

            WebClient client = new WebClient();
            Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

            foreach (var header in Headers)
                client.Headers.Add(header.Key, header.Value);

            client.Headers.Add("cache-control", $"max-age={options.CacheControl}");
            client.Headers.Add("content-type", options.ContentType);

            if (options.Upsert)
            {
                client.Headers.Add("x-upsert", options.Upsert.ToString().ToLower());
            }

            if (onProgress != null)
            {
                client.UploadProgressChanged += onProgress;
            }

            client.UploadFileCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    tsc.SetException(args.Error);
                }
                else
                {
                    tsc.SetResult(GetFinalPath(supabasePath));
                }
            };

            client.UploadFileAsync(uri, localPath);

            return tsc.Task;
        }

        private Task<string> UploadOrUpdate(byte[] data, string supabasePath, FileOptions options, UploadProgressChangedEventHandler onProgress = null)
        {
            var tsc = new TaskCompletionSource<string>();


            WebClient client = new WebClient();
            Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

            foreach (var header in Headers)
                client.Headers.Add(header.Key, header.Value);

            client.Headers.Add("cache-control", $"max-age={options.CacheControl}");
            client.Headers.Add("content-type", options.ContentType);

            if (options.Upsert)
            {
                client.Headers.Add("x-upsert", options.Upsert.ToString());
            }

            if (onProgress != null)
            {
                client.UploadProgressChanged += onProgress;
            }

            client.UploadDataCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    tsc.SetException(args.Error);
                }
                else
                {
                    tsc.SetResult(GetFinalPath(supabasePath));
                }
            };

            client.UploadDataAsync(uri, data);

            return tsc.Task;
        }

        private string GetFinalPath(string path) => $"{BucketId}/{path}";
    }
}

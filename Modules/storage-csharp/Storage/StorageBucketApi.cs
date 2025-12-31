using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Supabase.Core;
using Supabase.Core.Extensions;
using Supabase.Storage.Exceptions;
using Supabase.Storage.Interfaces;

namespace Supabase.Storage
{
    public class StorageBucketApi : IStorageBucketApi<Bucket>
    {
        public ClientOptions Options { get; protected set; }
        protected string Url { get; set; }

        private readonly Dictionary<string, string> _initializedHeaders;
        private Dictionary<string, string> _headers;
        public Dictionary<string, string> Headers
        {
            get
            {
                if (GetHeaders != null)
                    _headers = GetHeaders();

                return _headers.MergeLeft(_initializedHeaders);
            }
            set
            {
                _headers = value;

                if (!_headers.ContainsKey("X-Client-Info"))
                    _headers.Add("X-Client-Info", Util.GetAssemblyVersion(typeof(Client)));
            }
        }

        /// <summary>
        /// Function that can be set to return dynamic headers.
        /// 
        /// Headers specified in the constructor will ALWAYS take precendece over headers returned by this function.
        /// </summary>
        public Func<Dictionary<string, string>>? GetHeaders { get; set; }

        protected StorageBucketApi(string url, ClientOptions? options, Dictionary<string, string>? headers = null) : this(url, headers)
        {
            Options = options ?? new ClientOptions();
        }

        protected StorageBucketApi(string url, Dictionary<string, string>? headers = null)
        {
            Url = url;
            Options ??= new ClientOptions();

			// Initializes HttpClients with Timeouts to be Reused [Re: #8](https://github.com/supabase-community/storage-csharp/issues/8)
			Helpers.Initialize(Options);

            headers ??= new Dictionary<string, string>();
            _headers = headers;
            _initializedHeaders = headers;
        }

        /// <summary>
        /// Retrieves the details of all Storage buckets within an existing product.
        /// </summary>
        /// <returns></returns>
        public Task<List<Bucket>?> ListBuckets() =>
             Helpers.MakeRequest<List<Bucket>>(HttpMethod.Get, $"{Url}/bucket", null, Headers);

        /// <summary>
        /// Retrieves the details of an existing Storage bucket.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Bucket?> GetBucket(string id)
        {
            try
            {
                var result = await Helpers.MakeRequest<Bucket>(HttpMethod.Get, $"{Url}/bucket/{id}", null, Headers);
                return result;
            }
            catch (SupabaseStorageException ex)
            {
                if (ex.Reason == FailureHint.Reason.NotFound)
                    return null;
                else
                    throw;
            }
        }

        /// <summary>
        /// Creates a new Storage bucket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns>Bucket Id</returns>
        public async Task<string> CreateBucket(string id, BucketUpsertOptions? options = null)
        {
            options ??= new BucketUpsertOptions();

            var data = new Bucket
            {
                Id = id,
                Name = id,
                Public = options.Public,
                FileSizeLimit = options.FileSizeLimit,
                AllowedMimes = options.AllowedMimes
            };

            var result = await Helpers.MakeRequest<Bucket>(HttpMethod.Post, $"{Url}/bucket", data, Headers);

            return result?.Name!;
        }

        /// <summary>
        /// Updates a Storage bucket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<Bucket?> UpdateBucket(string id, BucketUpsertOptions? options = null)
        {
            options ??= new BucketUpsertOptions();

            var data = new Bucket
            {
                Id = id,
                Public = options.Public,
                FileSizeLimit = options.FileSizeLimit,
                AllowedMimes = options.AllowedMimes
            };

            var result = await Helpers.MakeRequest<Bucket>(HttpMethod.Put, $"{Url}/bucket/{id}", data, Headers);

            return result;
        }

        /// <summary>
        /// Removes all objects inside a single bucket.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<GenericResponse?> EmptyBucket(string id) =>
            Helpers.MakeRequest<GenericResponse>(HttpMethod.Post, $"{Url}/bucket/{id}/empty", null, Headers);

        /// <summary>
        /// Deletes an existing bucket. A bucket can't be deleted with existing objects inside it.
        /// You must first <see cref="EmptyBucket(string)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<GenericResponse?> DeleteBucket(string id) =>
            Helpers.MakeRequest<GenericResponse>(HttpMethod.Delete, $"{Url}/bucket/{id}", null, Headers);

    }
}

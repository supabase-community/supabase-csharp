using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Supabase.Storage
{
    public class StorageBucketApi
    {
        protected string Url { get; set; }
        protected Dictionary<string, string> Headers { get; set; }

        public StorageBucketApi(string url, Dictionary<string, string> headers = null)
        {
            Url = url;

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
        /// Retrieves the details of all Storage buckets within an existing product.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Bucket>> ListBuckets()
        {
            var result = await Helpers.MakeRequest<List<Bucket>>(HttpMethod.Get, $"{Url}/bucket", null, Headers);
            return result;
        }

        /// <summary>
        /// Retrieves the details of an existing Storage bucket.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Bucket> GetBucket(string id)
        {
            try
            {
                var result = await Helpers.MakeRequest<Bucket>(HttpMethod.Get, $"{Url}/bucket/{id}", null, Headers);
                return result;
            }
            catch (BadRequestException ex)
            {
                if (ex.ErrorResponse.Error == "Not found") return null;
                else throw ex;
            }
        }

        /// <summary>
        /// Creates a new Storage bucket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns>Bucket Id</returns>
        public async Task<string> CreateBucket(string id, BucketUpsertOptions options = null)
        {
            if (options == null)
            {
                options = new BucketUpsertOptions();
            }

            var data = new Bucket { Id = id, Name = id, Public = options.Public };
            var result = await Helpers.MakeRequest<Bucket>(HttpMethod.Post, $"{Url}/bucket", data, Headers);
            return result.Name;
        }

        /// <summary>
        /// Updates a Storage bucket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<Bucket> UpdateBucket(string id, BucketUpsertOptions options = null)
        {
            if (options == null)
            {
                options = new BucketUpsertOptions();
            }

            var data = new Bucket { Id = id, Public = options.Public };
            var result = await Helpers.MakeRequest<Bucket>(HttpMethod.Put, $"{Url}/bucket/{id}", data, Headers);
            return result;
        }

        /// <summary>
        /// Removes all objects inside a single bucket.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<GenericResponse> EmptyBucket(string id)
        {
            var result = await Helpers.MakeRequest<GenericResponse>(HttpMethod.Post, $"{Url}/bucket/{id}/empty", null, Headers);
            return result;
        }

        /// <summary>
        /// Deletes an existing bucket. A bucket can't be deleted with existing objects inside it.
        /// You must first <see cref="EmptyBucket(string)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<GenericResponse> DeleteBucket(string id)
        {
            var result = await Helpers.MakeRequest<GenericResponse>(HttpMethod.Delete, $"{Url}/bucket/{id}", null, Headers);
            return result;
        }

    }

    public class BucketUpsertOptions
    {
        [JsonProperty("public")]
        public bool Public { get; set; } = false;
    }
}

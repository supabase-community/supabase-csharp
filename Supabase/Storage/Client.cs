using System;
using System.Collections.Generic;

namespace Supabase.Storage
{
    public class Client : StorageBucketApi
    {
        public Client(string url, Dictionary<string, string> headers) : base(url, headers)
        { }

        /// <summary>
        /// Perform a file operation in a bucket
        /// </summary>
        /// <param name="id">Bucket Id</param>
        /// <returns></returns>
        public StorageFileApi From(string id) => new StorageFileApi(Url, Headers, id);
    }
}

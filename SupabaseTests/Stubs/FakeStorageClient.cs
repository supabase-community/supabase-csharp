using Storage.Interfaces;
using Supabase.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SupabaseTests.Stubs
{
    internal class FakeStorageClient : IStorageClient<Bucket, FileObject>
    {
        public Dictionary<string, string> Headers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Func<Dictionary<string, string>> GetHeaders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ClientOptions Options => throw new NotImplementedException();

		public Task<string> CreateBucket(string id, BucketUpsertOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<GenericResponse> DeleteBucket(string id)
        {
            throw new NotImplementedException();
        }

        public Task<GenericResponse> EmptyBucket(string id)
        {
            throw new NotImplementedException();
        }

        public IStorageFileApi<FileObject> From(string id)
        {
            throw new NotImplementedException();
        }

        public Task<Bucket> GetBucket(string id)
        {
            throw new NotImplementedException();
        }

        public Task<List<Bucket>> ListBuckets()
        {
            throw new NotImplementedException();
        }

        public Task<Bucket> UpdateBucket(string id, BucketUpsertOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}

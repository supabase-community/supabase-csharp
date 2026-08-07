namespace Supabase.Storage
{
    /// <summary>
    /// Options for <see cref="Interfaces.IStorageFileApi{TFileObject}.PurgeCache"/> and
    /// <see cref="Interfaces.IStorageBucketApi{TBucket}.PurgeBucketCache"/>.
    /// </summary>
    public class PurgeCacheOptions
    {
        /// <summary>
        /// If <c>true</c>, purges only the transformations (resized/formatted variants) for the object
        /// or bucket, leaving the original cached file intact. If left <c>null</c>, all cached versions
        /// are purged.
        /// </summary>
        public bool? Transformations { get; set; }
    }
}

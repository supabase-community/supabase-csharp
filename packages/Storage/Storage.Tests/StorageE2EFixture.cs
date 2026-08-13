using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace Storage.Tests;

/// <summary>
///     Base fixture for storage E2E tests that drive the live local stack. Every bucket a test touches is
///     created through <see cref="NewBucket" /> (or registered with <see cref="Track" />) and torn down in
///     <see cref="RemoveTrackedBuckets" /> no matter how the test ends. That keeps each run fresh and
///     standalone: a failed or interrupted test can no longer leak a bucket that blocks the next run — the
///     failure mode that a fixed-name "copyfile"/"parent" bucket caused before.
/// </summary>
public abstract class StorageE2EFixture
{
    private readonly List<string> trackedBuckets = new();

    /// <summary>Service-role client: full bucket administration.</summary>
    protected Client Storage { get; } = Helpers.GetServiceClient();

    /// <summary>Anon (public-key) client: used to assert what an unprivileged caller may and may not do.</summary>
    protected Client PublicStorage { get; } = Helpers.GetPublicClient();

    /// <summary>
    ///     Creates a uniquely-named bucket via the service client and registers it for teardown. Unique names
    ///     mean tests never collide on a shared id, so they run in any order and survive a previous failed run.
    /// </summary>
    protected async Task<string> NewBucket(BucketUpsertOptions? options = null)
    {
        var id = Guid.NewGuid().ToString();
        await this.Storage.CreateBucket(id, options);
        return this.Track(id);
    }

    /// <summary>Registers a bucket created elsewhere so teardown removes it too. Returns the id for chaining.</summary>
    protected string Track(string bucketId)
    {
        this.trackedBuckets.Add(bucketId);
        return bucketId;
    }

    [TestCleanup]
    public async Task RemoveTrackedBuckets()
    {
        foreach (var id in this.trackedBuckets)
        {
            await TryEmpty(this.Storage, id);
            await TryDelete(this.Storage, id);
        }
        this.trackedBuckets.Clear();
    }

    private static async Task TryEmpty(Client storage, string id)
    {
        try
        {
            await storage.EmptyBucket(id);
        }
        catch (SupabaseStorageException) {  }
    }

    private static async Task TryDelete(Client storage, string id)
    {
        try
        {
            await storage.DeleteBucket(id);
        }
        catch (SupabaseStorageException)  { }
    }
}

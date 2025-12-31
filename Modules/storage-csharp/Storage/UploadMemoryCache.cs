using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Supabase.Storage;

/// <summary>
/// Provides thread-safe in-memory caching for resumable upload URLs with sliding expiration.
/// </summary>
public class UploadMemoryCache
{
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private static TimeSpan _defaultTtl = TimeSpan.FromMinutes(60);

    private static long _version; // helps with testing/observability if needed

    private sealed class CacheEntry
    {
        public string Url { get; }
        public DateTimeOffset Expiration { get; private set; }
        public TimeSpan Ttl { get; }

        public CacheEntry(string url, TimeSpan ttl)
        {
            Url = url;
            Ttl = ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl;
            Touch();
        }

        public void Touch()
        {
            Expiration = DateTimeOffset.UtcNow.Add(Ttl);
        }

        public bool IsExpired() => DateTimeOffset.UtcNow >= Expiration;
    }

    /// <summary>
    /// Sets the default time-to-live duration for future cache entries.
    /// </summary>
    /// <param name="ttl">The time-to-live duration. If less than or equal to zero, defaults to 5 minutes.</param>
    public static void SetDefaultTtl(TimeSpan ttl)
    {
        _defaultTtl = ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl;
    }

    // Store or upate the resumable upload URL for the provided key.
    /// <summary>
    /// Stores or updates a resumable upload URL in the cache for the specified key.
    /// </summary>
    /// <param name="key">The unique identifier for the cached URL.</param>
    /// <param name="url">The resumable upload URL to cache.</param>
    /// <param name="ttl">Optional time-to-live duration. If not specified, uses the default TTL.</param>
    /// <exception cref="ArgumentException">Thrown when key or url is null, empty, or whitespace.</exception>
    public static void Set(string key, string url, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key must be provided.", nameof(key));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url must be provided.", nameof(url));

        var entryTtl = ttl.GetValueOrDefault(_defaultTtl);
        _cache.AddOrUpdate(
            key,
            _ => new CacheEntry(url, entryTtl),
            (_, existing) => new CacheEntry(url, entryTtl)
        );

        Interlocked.Increment(ref _version);
        CleanupIfNeeded();
    }

    /// <summary>
    /// Attempts to retrieve a cached URL by its key. Updates the sliding expiration on successful retrieval.
    /// </summary>
    /// <param name="key">The unique identifier for the cached URL.</param>
    /// <param name="url">When this method returns, contains the cached URL if found; otherwise, null.</param>
    /// <returns>True if the URL was found in the cache; otherwise, false.</returns>
    public static bool TryGet(string key, out string? url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!_cache.TryGetValue(key, out var entry)) return false;
        if (entry.IsExpired())
        {
            _cache.TryRemove(key, out _);
            return false;
        }

        entry.Touch();
        url = entry.Url;
        return true;

    }

    /// <summary>
    /// Removes a cached URL by its key.
    /// </summary>
    /// <param name="key">The unique identifier for the cached URL to remove.</param>
    /// <returns>True if the URL was successfully removed; otherwise, false.</returns>
    public static bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var removed = _cache.TryRemove(key, out _);
        if (removed)
            Interlocked.Increment(ref _version);
        return removed;
    }

    /// <summary>
    /// Removes all cached URLs from the cache.
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
        Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// Gets the current number of entries in the cache.
    /// </summary>
    public static int Count => _cache.Count;

    private static void CleanupIfNeeded()
    {
        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired())
            {
                _cache.TryRemove(kvp.Key, out _);
            }
        }
    }
}
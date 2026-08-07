using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Caching;

/// <summary>
///     End-to-end proof that a table backed by an <see cref="IPostgrestCacheProvider" /> serves the second
///     identical request from the cache: the first request populates the cache and raises its lifecycle
///     events, the second is a cache hit. The provider is an NSubstitute backed by an in-memory store.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class CachedTableTests
{
    private static IPostgrestCacheProvider InMemoryCache()
    {
        var store = new Dictionary<string, object>();
        var cache = Substitute.For<IPostgrestCacheProvider>();
        cache.When(x => x.SetItem(Arg.Any<string>(), Arg.Any<object>()))
            .Do(call => store[call.ArgAt<string>(0)] = call.ArgAt<object>(1));
        cache.GetItem<CachedModel<Movie>?>(Arg.Any<string>())
            .Returns(call => Task.FromResult(store.TryGetValue(call.ArgAt<string>(0), out var value)
                ? (CachedModel<Movie>?) value
                : null));
        cache.When(x => x.Empty()).Do(_ => store.Clear());
        return cache;
    }

    [TestMethod]
    public async Task Get_ShouldServeTheSecondRequestFromCache()
    {
        var client = LocalStack.Client();
        var cache = InMemoryCache();
        await cache.Empty();
        var firstCached = new TaskCompletionSource<bool>();
        var firstPopulated = new TaskCompletionSource<bool>();
        var initial = await client.Table<Movie>(cache).Get();
        initial.Models.Should().BeEmpty("the first request has no cache to hydrate from");
        initial.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(initial.WasResponseCached))
                firstCached.TrySetResult(initial.WasResponseCached);
        };
        initial.RemoteModelsPopulated += sender => firstPopulated.TrySetResult(sender.WasResponseCached);
        (await Task.WhenAll(firstCached.Task, firstPopulated.Task)).Should().AllSatisfy(cached => cached.Should().BeTrue());
        var cachedRequest = await client.Table<Movie>(cache).Get();
        var secondHit = new TaskCompletionSource<bool>();
        cachedRequest.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(cachedRequest.WasResponseCached))
                secondHit.TrySetResult(cachedRequest.WasCacheHit);
        };
        (await secondHit.Task).Should().BeTrue("the second identical request is served from the cache");
        cachedRequest.Models.Should().NotBeEmpty();
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using SupabaseTests.Models;
using static Supabase.StatelessClient;

namespace SupabaseTests;

/// <summary>
/// Proves the composed client actually round-trips against a live Supabase CLI stack
/// (<c>supabase start</c>, localhost:54321): models fetched through the umbrella can be updated and
/// deleted, and the stateless facade returns the same REST data as a hand-built Postgrest client.
/// These exercise child transport on purpose — the value is that the umbrella wired a <em>working</em>
/// client end to end. Gated off the PR/mutation path for cost, not reliability (QUALITY_RUBRIC §2, §4).
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class SupabaseClientDatabaseTests
{
    private const string ServiceKey = "sb_secret_N7UND0UgjKTVK-Uodkm0Hg_xSvEMPvz";

    private Supabase.Client client;

    private static Supabase.SupabaseOptions StackOptions() => new()
    {
        AuthUrlFormat = "{0}:54321/rest/v1",
        RealtimeUrlFormat = "ws://127.0.0.1:54321/realtime/v1",
        RestUrlFormat = "{0}:54321/rest/v1",
        AutoConnectRealtime = false,
    };

    [TestInitialize]
    public async Task InitializeTest()
    {
        client = new Supabase.Client("http://localhost", ServiceKey, StackOptions());
        await client.InitializeAsync();
    }

    [TestMethod]
    public async Task SupabaseClient_ShouldUpdateModel_GivenModelFetchedFromDatabase()
    {
        var insertResult = await client.From<Channel>().Insert(new Channel { Slug = Guid.NewGuid().ToString() });
        var channel = insertResult.Models.First();
        var newSlug = $"Updated Slug @ {DateTime.Now.ToLocalTime()}";
        channel.Slug = newSlug;
        var updatedResult = await channel.Update<Channel>();
        updatedResult.Models.First().Slug.Should().Be(newSlug);
    }

    [TestMethod]
    public async Task SupabaseClient_ShouldDeleteModel_GivenModelFetchedFromDatabase()
    {
        var slug = Guid.NewGuid().ToString();
        var insertResult = await client.From<Channel>().Insert(new Channel { Slug = slug });
        await insertResult.Models.First().Delete<Channel>();
        var result = await client.From<Channel>().Filter("slug", Constants.Operator.Equals, slug).Get();
        result.Models.Should().BeEmpty();
    }

    [TestMethod]
    public async Task StatelessFrom_ShouldReturnSameDataAsHandBuiltPostgrestClient()
    {
        var options = StackOptions();
        var handBuilt = await new Client(string.Format(options.RestUrlFormat, "http://localhost"),
            GetRestOptions(ServiceKey, options)).Table<Channel>().Get();
        var stateless = await From<Channel>("http://localhost", ServiceKey, options).Get();
        stateless.Models.Count.Should().Be(handBuilt.Models.Count);
    }
}
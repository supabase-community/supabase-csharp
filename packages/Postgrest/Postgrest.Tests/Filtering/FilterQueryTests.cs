using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Filtering;

/// <summary>
///     End-to-end proof that each filter operator selects the same rows against a live PostgREST as the
///     equivalent LINQ predicate does in memory — the outer-loop oracle for the hermetic
///     <see cref="FilterSerializationTests" />.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class FilterQueryTests
{
    private static async Task<List<User>> AllUsers() => (await LocalStack.Client().Table<User>().Get()).Models;

    [TestMethod]
    public async Task Not_ShouldExcludeMatchingRows()
    {
        var filter = new Supabase.Postgrest.QueryFilter("username", Operator.Equals, "supabot");
        var filtered = await LocalStack.Client().Table<User>().Not(filter).Get();
        filtered.Models.Should().Equal((await AllUsers()).Where(u => u.Username != "supabot").ToList());
    }

    [TestMethod]
    public async Task NotShorthand_ShouldExcludeMatchingRows()
    {
        var filtered = await LocalStack.Client().Table<User>().Not("username", Operator.Equals, "supabot").Get();
        filtered.Models.Should().Equal((await AllUsers()).Where(u => u.Username != "supabot").ToList());
    }

    [TestMethod]
    public async Task NotIn_ShouldExcludeAllListedRows()
    {
        var response = await LocalStack.Client().Table<User>()
            .Not("username", Operator.In, new List<string> { "supabot", "kiwicopple" }).Get();
        response.Models.Should().Equal((await AllUsers())
            .Where(u => u.Username != "supabot" && u.Username != "kiwicopple").ToList());
    }

    [TestMethod]
    public async Task FilterEquals_ShouldSelectTheSingleMatch()
    {
        var filtered = await LocalStack.Client().Table<User>().Filter("username", Operator.Equals, "supabot").Get();
        filtered.Models.Should().ContainSingle().Which.Username.Should().Be("supabot");
    }

    [TestMethod]
    public async Task FilterIsNull_ShouldSelectRowsWithNullColumn()
    {
        var client = LocalStack.Client();
        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });
        var filtered = await client.Table<User>().Filter<string>("catchphrase", Operator.Is, null).Get();
        filtered.Models.Should().Equal((await AllUsers()).Where(u => u.Catchphrase == null).ToList());
    }

    [TestMethod]
    public async Task FilterEqualsNull_ShouldSelectRowsWithNullColumn()
    {
        var client = LocalStack.Client();
        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });
        var filtered = await client.Table<User>().Filter<string>("catchphrase", Operator.Equals, null).Get();
        filtered.Models.Should().Equal((await AllUsers()).Where(u => u.Catchphrase == null).ToList());
    }

    [TestMethod]
    public async Task FilterNotEqualsNull_ShouldSelectRowsWithANonNullColumn()
    {
        var client = LocalStack.Client();
        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });
        var filtered = await client.Table<User>().Filter<string>("catchphrase", Operator.NotEqual, null).Get();
        filtered.Models.Should().Equal((await AllUsers()).Where(u => u.Catchphrase != null).ToList());
    }

    [TestMethod]
    public async Task FilterIn_ShouldSelectRowsInTheList()
    {
        var criteria = new List<object> { "supabot", "kiwicopple" };
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("username", Operator.In, criteria).Order("username", Ordering.Descending).Get();
        filtered.Models.Should().Equal((await AllUsers()).OrderByDescending(u => u.Username)
            .Where(u => u.Username is "supabot" or "kiwicopple").ToList());
    }

    [TestMethod]
    public async Task FilterGreaterThan_ShouldSelectRowsAboveTheBound()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("id", Operator.GreaterThan, "1").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages.Where(m => m.Id > 1).ToList());
    }

    [TestMethod]
    public async Task FilterGreaterThanOrEqual_ShouldSelectRowsAtOrAboveTheBound()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("id", Operator.GreaterThanOrEqual, "1").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages.Where(m => m.Id >= 1).ToList());
    }

    [TestMethod]
    public async Task FilterLessThan_ShouldSelectRowsBelowTheBound()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("id", Operator.LessThan, "2").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages.Where(m => m.Id < 2).ToList());
    }

    [TestMethod]
    public async Task FilterLessThanOrEqual_ShouldSelectRowsAtOrBelowTheBound()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("id", Operator.LessThanOrEqual, "2").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages.Where(m => m.Id <= 2).ToList());
    }

    [TestMethod]
    public async Task FilterNotEqual_ShouldExcludeTheMatchingRow()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("id", Operator.NotEqual, "2").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages.Where(m => m.Id != 2).ToList());
    }

    [TestMethod]
    public async Task FilterLike_ShouldSelectRowsMatchingThePattern()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("username", Operator.Like, "s%").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages.Where(m => m.UserName!.StartsWith("s")).ToList());
    }

    [TestMethod]
    public async Task FilterILike_ShouldSelectRowsMatchingCaseInsensitively()
    {
        var filtered = await LocalStack.Client().Table<Message>().Filter("username", Operator.ILike, "%SUPA%").Get();
        var messages = (await LocalStack.Client().Table<Message>().Get()).Models;
        filtered.Models.Should().Equal(messages
            .Where(m => m.UserName!.Contains("SUPA", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    [TestMethod]
    public async Task FilterContains_ShouldSelectRowsWhoseRangeContainsTheArgument()
    {
        var client = LocalStack.Client();
        await client.Table<User>().Insert(
            new User { Username = "skikra", Status = "ONLINE", AgeRange = new IntRange(1, 3) },
            new QueryOptions { Upsert = true });
        var filtered = await client.Table<User>().Filter("age_range", Operator.Contains, new IntRange(1, 2)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value <= 1 && m.AgeRange?.End.Value >= 2).ToList());
    }

    [TestMethod]
    public async Task FilterContainedIn_ShouldSelectRowsWhoseRangeSitsInsideTheArgument()
    {
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("age_range", Operator.ContainedIn, new IntRange(25, 35)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value >= 25 && m.AgeRange?.End.Value <= 35).ToList());
    }

    [TestMethod]
    public async Task FilterStrictlyLeft_ShouldSelectRowsEntirelyBelowTheArgument()
    {
        var client = LocalStack.Client();
        await client.Table<User>().Insert(
            new User { Username = "minds3t", Status = "ONLINE", AgeRange = new IntRange(3, 6) },
            new QueryOptions { Upsert = true });
        var filtered = await client.Table<User>().Filter("age_range", Operator.StrictlyLeft, new IntRange(7, 8)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value < 7 && m.AgeRange?.End.Value < 7).ToList());
    }

    [TestMethod]
    public async Task FilterStrictlyRight_ShouldSelectRowsEntirelyAboveTheArgument()
    {
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("age_range", Operator.StrictlyRight, new IntRange(1, 2)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value > 2 && m.AgeRange?.End.Value > 2).ToList());
    }

    [TestMethod]
    public async Task FilterNotLeftOf_ShouldSelectRowsThatDoNotExtendLeftOfTheArgument()
    {
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("age_range", Operator.NotLeftOf, new IntRange(2, 4)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value >= 2 && m.AgeRange?.End.Value >= 2).ToList());
    }

    [TestMethod]
    public async Task FilterNotRightOf_ShouldSelectRowsThatDoNotExtendRightOfTheArgument()
    {
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("age_range", Operator.NotRightOf, new IntRange(2, 4)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value <= 4).ToList());
    }

    [TestMethod]
    public async Task FilterAdjacent_ShouldSelectRowsBorderingTheArgument()
    {
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("age_range", Operator.Adjacent, new IntRange(1, 2)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.End.Value == 0 || m.AgeRange?.Start.Value == 3).ToList());
    }

    [TestMethod]
    public async Task FilterOverlap_ShouldSelectRowsWhoseRangeIntersectsTheArgument()
    {
        var filtered = await LocalStack.Client().Table<User>()
            .Filter("age_range", Operator.Overlap, new IntRange(2, 4)).Get();
        filtered.Models.Should().Equal((await AllUsers())
            .Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value >= 2).ToList());
    }

    [TestMethod]
    public async Task FilterFullTextSearch_ShouldMatchTheIndexedDocument()
    {
        var config = new FullTextSearchConfig("'fat' & 'cat'", "english");
        var filtered = await LocalStack.Client().Table<User>().Filter("catchphrase", Operator.FTS, config).Get();
        filtered.Models.Should().ContainSingle().Which.Username.Should().Be("supabot");
    }

    [TestMethod]
    public async Task FilterPlainFullTextSearch_ShouldMatchTheIndexedDocument()
    {
        var config = new FullTextSearchConfig("'fat' & 'cat'", "english");
        var filtered = await LocalStack.Client().Table<User>().Filter("catchphrase", Operator.PLFTS, config).Get();
        filtered.Models.Should().ContainSingle().Which.Username.Should().Be("supabot");
    }

    [TestMethod]
    public async Task FilterWebFullTextSearch_ShouldMatchTheIndexedDocument()
    {
        var config = new FullTextSearchConfig("'fat' & 'cat'", "english");
        var filtered = await LocalStack.Client().Table<User>().Filter("catchphrase", Operator.WFTS, config).Get();
        filtered.Models.Should().ContainSingle().Which.Username.Should().Be("supabot");
    }

    [TestMethod]
    public async Task FilterPhraseFullTextSearch_ShouldMatchRowsContainingThePhrase()
    {
        var client = LocalStack.Client();
        var config = new FullTextSearchConfig("'cat'", "english");
        var filtered = await client.Table<User>().Filter("catchphrase", Operator.PHFTS, config).Get();
        var nonNull = await client.Table<User>().Filter<string>("catchphrase", Operator.NotEqual, null).Get();
        filtered.Models.Should().Equal(nonNull.Models.Where(u => u.Catchphrase!.Contains("'cat'")).ToList());
    }

    [TestMethod]
    public async Task FilterMatch_ShouldApplyEveryColumnEquality()
    {
        var client = LocalStack.Client();
        var expected = (await client.Table<User>().Get()).Models
            .Where(u => u.Username == "kiwicopple" && u.Status == "OFFLINE").ToList();
        var filtered = await client.Table<User>()
            .Match(new Dictionary<string, string> { { "username", "kiwicopple" }, { "status", "OFFLINE" } }).Get();
        filtered.Models.Should().Equal(expected);
    }

    [TestMethod]
    public async Task FilterDateTime_ShouldSelectRowsInsideTheWindow()
    {
        var filtered = await LocalStack.Client().Table<Movie>()
            .Filter("created_at", Operator.GreaterThan, new DateTime(2022, 08, 20))
            .Filter("created_at", Operator.LessThan, new DateTime(2022, 08, 21)).Get();
        filtered.Models.Should().ContainSingle()
            .Which.Id.Should().Be("ea07bd86-a507-4c68-9545-b848bfe74c90");
    }

    [TestMethod]
    public async Task FilterDateTimeOffset_ShouldSelectRowsInsideTheWindow()
    {
        var filtered = await LocalStack.Client().Table<Movie>()
            .Filter("created_at", Operator.GreaterThan, new DateTimeOffset(new DateTime(2022, 08, 20)))
            .Filter("created_at", Operator.LessThan, new DateTimeOffset(new DateTime(2022, 08, 21))).Get();
        filtered.Models.Should().ContainSingle()
            .Which.Id.Should().Be("ea07bd86-a507-4c68-9545-b848bfe74c90");
    }

    [TestMethod]
    public async Task FilterLong_ShouldSelectRowsMatchingThe64BitValue()
    {
        var filtered = await LocalStack.Client().Table<KitchenSink>()
            .Filter("long_value", Operator.Equals, 2147483648L).Get();
        filtered.Models.Should().ContainSingle();
    }
}

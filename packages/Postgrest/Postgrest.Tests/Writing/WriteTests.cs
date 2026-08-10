using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Writing;

/// <summary>
///     Round-trips of the "write" verbs against a live Postgrest: insert (single, bulk, minimal-return),
///     update, upsert (including on-conflict resolution), a primary-key conflict, and column-scoped updates.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class WriteTests
{
    [TestMethod]
    public async Task Update_ShouldPersistAndReturnTheChangedRow()
    {
        var client = LocalStack.Client();
        var user = await client.Table<User>().Filter("username", Operator.Equals, "supabot").Single(this.TestContext.CancellationToken);
        user.Should().NotBeNull();
        user.Status = "OFFLINE";
        var response = await user.Update<User>(this.TestContext.CancellationToken);
        response.Models.Should().ContainSingle();
        response.Models.First().Status.Should().Be("OFFLINE");
    }

    [TestMethod]
    public async Task Insert_ShouldReturnTheInsertedRow()
    {
        var client = LocalStack.Client();
        var newUser = new User
        {
            Username = Guid.NewGuid().ToString(),
            AgeRange = new IntRange(18, 22),
            Catchphrase = "what a shot",
            Status = "ONLINE"
        };
        var response = await client.Table<User>().Insert(newUser, cancellationToken: this.TestContext.CancellationToken);
        var inserted = response.Models.Single();
        inserted.Username.Should().Be(newUser.Username);
        inserted.AgeRange.Should().Be(newUser.AgeRange);
        await client.Table<User>().Delete(newUser, cancellationToken: this.TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Insert_ShouldReturnEmptyContent_GivenMinimalReturn()
    {
        var client = LocalStack.Client();
        var newUser = new User { Username = Guid.NewGuid().ToString(), Status = "ONLINE" };
        var response = await client.Table<User>()
            .Insert(newUser, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal }, this.TestContext.CancellationToken);
        response.Content.Should().BeEmpty();
        await client.Table<User>().Delete(newUser, cancellationToken: this.TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Insert_ShouldThrow_GivenAPrimaryKeyConflictWithoutUpsert()
    {
        var client = LocalStack.Client();
        var act = () => client.Table<User>().Insert(new User { Username = "supabot" }, cancellationToken: this.TestContext.CancellationToken);
        await act.Should().ThrowAsync<PostgrestException>();
    }

    [TestMethod]
    public async Task Upsert_ShouldReturnTheResolvedRow()
    {
        var client = LocalStack.Client();
        var model = new User
        {
            Username = "supabot",
            AgeRange = new IntRange(3, 8),
            Status = "OFFLINE",
            Catchphrase = "fat cat"
        };
        var response = await client.Table<User>().Insert(model, new QueryOptions { Upsert = true }, this.TestContext.CancellationToken);
        var updated = response.Models.Single();
        updated.Username.Should().Be("supabot");
        updated.Status.Should().Be("OFFLINE");
    }

    [TestMethod]
    public async Task Upsert_ShouldResolveOnAConflictColumn()
    {
        var client = LocalStack.Client();
        var kitchenSink = new KitchenSink { Id = Guid.NewGuid(), UniqueValue = "Testing" };
        var inserted = await client.Table<KitchenSink>().OnConflict("unique_value").Upsert(kitchenSink, cancellationToken: this.TestContext.CancellationToken);
        var updated = await client.Table<KitchenSink>()
            .OnConflict(x => x.UniqueValue!)
            .Set(x => x.StringValue!, "Testing 1")
            .Upsert(inserted.Models.First(), cancellationToken: this.TestContext.CancellationToken);
        updated.Models.Should().ContainSingle()
            .Which.UniqueValue.Should().Be("Testing", "the upsert resolves onto the existing unique_value row");
    }

    [TestMethod]
    public async Task Insert_ShouldReturnEveryRow_GivenABulkInsert()
    {
        var client = LocalStack.Client();
        var users = new List<User>
        {
            new() { Username = "rocket", AgeRange = new IntRange(35, 40), Status = "ONLINE" },
            new() { Username = "ace", AgeRange = new IntRange(21, 28), Status = "OFFLINE" }
        };
        var response = await client.Table<User>().Insert(users, cancellationToken: this.TestContext.CancellationToken);
        response.Models.Should().Equal(users);
        await client.Table<User>().Delete(users[0], cancellationToken: this.TestContext.CancellationToken);
        await client.Table<User>().Delete(users[1], cancellationToken: this.TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Insert_ShouldRoundTripIntArrays()
    {
        var numbers = new List<int> { 1, 2, 3 };
        var result = await LocalStack.Client().Table<User>().Insert(new User
        {
            Username = "WALRUS", Status = "ONLINE", Catchphrase = "I'm a walrus",
            FavoriteNumbers = numbers, AgeRange = new IntRange(15, 25)
        }, new QueryOptions { Upsert = true }, this.TestContext.CancellationToken);
        result.Models.First().FavoriteNumbers.Should().Equal(numbers);
    }

    [TestMethod]
    public async Task Delete_ShouldReturnTheDeletedRow()
    {
        var client = LocalStack.Client();
        var user = new User { Username = Guid.NewGuid().ToString(), Status = "ONLINE" };
        await client.Table<User>().Insert(user, cancellationToken: this.TestContext.CancellationToken);
        var response = await client.Table<User>().Filter("username", Operator.Equals, user.Username).Delete(cancellationToken: this.TestContext.CancellationToken);
        response.Models.Should().ContainSingle().Which.Username.Should().Be(user.Username);
    }

    [TestMethod]
    public async Task Delete_ShouldReturnNoModels_GivenNothingMatched()
    {
        var client = LocalStack.Client();
        var response = await client.Table<User>()
            .Filter("username", Operator.Equals, Guid.NewGuid().ToString()).Delete(cancellationToken: this.TestContext.CancellationToken);
        response.Models.Should().BeEmpty(
            "a delete that matches nothing succeeds with an empty representation rather than reporting a phantom success (issue #91)");
    }

    [TestMethod]
    public async Task Update_ShouldOnlyWriteTheColumnsListed_GivenColumnsScope()
    {
        var client = LocalStack.Client();
        var movie = (await client.Table<Movie>().Get(this.TestContext.CancellationToken)).Models.First();
        var originalDate = movie.CreatedAt;
        var newName = $"{movie.Name} (Changed)";
        movie.Name = newName;
        movie.CreatedAt = DateTime.UtcNow;
        var result = await client.Table<Movie>().Columns(["name"]).Update(movie, cancellationToken: this.TestContext.CancellationToken);
        result.Models.First().CreatedAt.Should().Be(originalDate);
        result.Models.First().Name.Should().Be(newName);
    }

    public TestContext TestContext { get; set; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.References;

/// <summary>
///     End-to-end proof of foreign-table hydration: a root model comes back with its linked models (including
///     the same foreign table referenced more than once and one level of circular reference), links can be
///     created across tables, and reference join columns are omitted from update/delete select clauses.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class ReferenceTests
{
    [TestMethod]
    public async Task Get_ShouldHydrateLinkedModelsOnARootModel()
    {
        var client = LocalStack.Client();
        var movies = await client.Table<Movie>().Order(x => x.Id, Ordering.Ascending).Get();
        movies.Models.Should().NotBeEmpty();
        var topGun = movies.Models.First(x => x.Name!.Contains("Top Gun"));
        topGun.People.Should().NotBeEmpty();
        topGun.People.First().Profile.Should().NotBeNull();
        var bob = await client.Table<Person>().Filter("first_name", Operator.Equals, "Bob").Single();
        bob!.Profile!.Email.Should().Contain("bob");
        var product = await client.Table<Product>()
            .Filter("id", Operator.Equals, "8b8e89a0-63c7-4917-8dc1-7797dc0285f1").Single();
        product!.Name.Should().Be("product 1");
        product.Category!.Name.Should().Be("category 1");
        var products = await client.Table<Product>().Get();
        products.Models.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task Insert_ShouldCreateLinkedRecordsAcrossTables()
    {
        var client = LocalStack.Client();
        var movieModel = (await client.Table<Movie>().Insert(new Movie { Name = "Supabase in Action" })).Model;
        movieModel.Should().NotBeNull();
        var people = await client.Table<Person>().Insert(new List<Person>
        {
            new() { FirstName = "John", LastName = "Doe" },
            new() { FirstName = "Jane", LastName = "Buck" }
        });
        people.Models.Should().HaveCount(2);
        await client.Table<Profile>().Insert(new List<Profile>
        {
            new() { PersonId = people.Models[0].Id, Email = "john.doe@email.com" },
            new() { PersonId = people.Models[1].Id, Email = "jane.buck@email.com" }
        });
        await client.Table<MoviePerson>().Insert(new List<MoviePerson>
        {
            new() { PersonId = people.Models[0].Id, MovieId = movieModel!.Id },
            new() { PersonId = people.Models[1].Id, MovieId = movieModel.Id }
        });
        var relations = (await client.Table<Movie>().Where(x => x.Id == movieModel!.Id).Get()).Model!;
        relations.People.Should().Contain(x => x.Id == people.Models[0].Id);
        relations.People[0].Movies.Should().Contain(x => x.Id == movieModel!.Id);
        relations.People[0].Movies[0].People.Should().BeEmpty("circular references return only one layer");
        relations.People[0].Profile!.Person.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Get_ShouldReferenceTheSameForeignTableMultipleTimes()
    {
        var response = await LocalStack.Client().Table<ForeignKeyTestModel>().Get();
        response.Models.Should().NotBeEmpty();
        response.Model!.MovieFK1.Should().BeOfType<Movie>();
        response.Model.MovieFK2.Should().BeOfType<Movie>();
        response.Model.RandomPersonFK.Should().BeOfType<Person>();
    }

    [TestMethod]
    public async Task Get_ShouldReferenceANestedModelWithTheSameForeignTableMultipleTimes()
    {
        var response = await LocalStack.Client().Table<NestedForeignKeyTestModel>().Get();
        response.Models.Should().NotBeEmpty();
        response.Model!.User.Should().BeOfType<User>();
        response.Model.FKTestModel.Should().BeOfType<ForeignKeyTestModel>();
    }

    [TestMethod]
    public async Task UpdateAndDelete_ShouldNotIncludeReferenceJoinColumnsInSelect()
    {
        var client = LocalStack.Client();
        string? updateUrl = null;
        string? deleteUrl = null;
        OnRequestPreparedEventHandler handler = (_, _, method, url, _, _, _) =>
        {
            if (method == new HttpMethod("PATCH")) updateUrl = url;
            else if (method == HttpMethod.Delete) deleteUrl = url;
        };
        client.AddRequestPreparedHandler(handler);
        try
        {
            var inserted = await client.Table<Movie>().Insert(new Movie { Name = "Reference Column Test" });
            inserted.Model!.Name = "Reference Column Test Updated";
            await inserted.Model.Update<Movie>();
            await client.Table<Movie>().Filter("id", Operator.Equals, inserted.Model.Id).Delete();
        }
        finally
        {
            client.ClearRequestPreparedHandlers();
        }
        Uri.UnescapeDataString(updateUrl!).Should().NotContain(",person(", "PATCH must not join reference columns");
        Uri.UnescapeDataString(deleteUrl!).Should().NotContain(",person(", "DELETE must not join reference columns");
    }

    [TestMethod]
    public async Task Update_ShouldPersistScalarChanges_GivenPopulatedReferences()
    {
        var client = LocalStack.Client();
        var inserted = await client.Table<Movie>().Insert(new Movie
        {
            Name = "The Blue Eyed Samurai (Movie)",
            Status = MovieStatus.OffDisplay,
            People =
            [
                new Person { FirstName = "Maya", LastName = "Erskine" },
                new Person { FirstName = "Masi", LastName = "Oka" }
            ]
        });
        inserted.Model!.Status.Should().Be(MovieStatus.OffDisplay);
        inserted.Model.Status = MovieStatus.OnDisplay;
        var updated = await inserted.Model.Update<Movie>();
        updated.Model!.Status.Should().Be(MovieStatus.OnDisplay);
    }
}

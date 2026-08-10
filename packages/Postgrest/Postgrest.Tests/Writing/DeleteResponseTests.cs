using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Writing;

/// <summary>
///     A "delete" blocked by RLS (or matching nothing) is indistinguishable at the wire from a real delete except
///     by the rows it returns: Postgrest filters non-visible rows out silently and answers success with an empty
///     representation. These pin, at the request/response level, that the parameterless <c>Delete</c> hands that
///     representation back so the caller can tell "nothing was deleted" apart from "deleted" — see issue #91.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class DeleteResponseTests
{
    private WireMockServer server = null!;
    private Client client = null!;

    [Table("todos")]
    private class Todo : BaseModel
    {
        [PrimaryKey("id")] public int Id { get; set; }
        [Column("name")] public string? Name { get; set; }
    }

    [TestInitialize]
    public void SetUp()
    {
        this.server = WireMockServer.Start();
        this.client = new Client(this.server.Url!, new ClientOptions());
    }

    [TestCleanup]
    public void TearDown() => this.server.Stop();

    [TestMethod]
    public async Task Delete_ShouldReturnNoModels_GivenTheRowWasBlockedByRlsOrUnmatched()
    {
        this.MockDeleteReturns("[]");
        var response = await this.client.Table<Todo>().Filter("id", Operator.Equals, "1").Delete(cancellationToken: this.TestContext.CancellationToken);
        response.Models.Should().BeEmpty(
            "a delete filtered out by RLS returns success with an empty representation, and the caller must be able to see that nothing was deleted (issue #91)");
    }

    [TestMethod]
    public async Task Delete_ShouldReturnTheDeletedRows_GivenTheDeleteMatched()
    {
        this.MockDeleteReturns("[{\"id\":1,\"name\":\"walk the dog\"}]");
        var response = await this.client.Table<Todo>().Filter("id", Operator.Equals, "1").Delete();
        response.Models.Should().ContainSingle()
            .Which.Name.Should().Be("walk the dog", "the representation of the deleted row is returned to the caller");
    }

    private void MockDeleteReturns(string body) =>
        this.server.Given(Request.Create().WithPath("/todos").UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

    public TestContext TestContext { get; set; }
}

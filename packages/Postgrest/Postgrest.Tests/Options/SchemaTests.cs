using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Options;

/// <summary>
///     A client configured with a non-public <see cref="ClientOptions.Schema" /> resolves tables in that
///     schema, so a query hits the schema-scoped rows rather than the public ones.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class SchemaTests
{
    [TestMethod]
    public async Task Get_ShouldResolveTablesInTheConfiguredSchema()
    {
        var client = LocalStack.Client(new ClientOptions { Schema = "personal" });
        var response = await client.Table<User>().Filter(x => x.Username!, Operator.Equals, "leroyjenkins").Get();
        response.Models.Should().ContainSingle().Which.Username.Should().Be("leroyjenkins");
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Support;

namespace Postgrest.Tests.Rpc;

/// <summary>
///     Invoking Postgres functions through <c>Client.Rpc</c>: a scalar parameter and a composite row
///     parameter, asserting the call succeeds and returns the function's body.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class StoredProcedureTests
{
    [TestMethod]
    public async Task Rpc_ShouldInvokeAFunction_GivenAScalarParameter()
    {
        var response = await LocalStack.Client()
            .Rpc("get_status", new Dictionary<string, object> { { "name_param", "supabot" } });
        response.ResponseMessage!.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().Contain("OFFLINE");
    }

    [TestMethod]
    public async Task Rpc_ShouldInvokeAFunction_GivenACompositeRowParameter()
    {
        var parameters = new Dictionary<string, object>
        {
            { "param", new Dictionary<string, object> { { "username", "supabot" } } }
        };
        var response = await LocalStack.Client().Rpc("get_data", parameters);
        response.ResponseMessage!.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().Be("null");
    }
}

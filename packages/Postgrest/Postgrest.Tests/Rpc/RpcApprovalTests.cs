using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Support;

namespace Postgrest.Tests.Rpc;

/// <summary>
///     Pins the exact bytes an RPC call puts on the wire: the parameters object is serialized into the POST
///     body keyed by parameter name. This is the transport contract the System.Text.Json migration must
///     preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class RpcApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task RpcRequest_ShouldSerializeParametersToTheBody_GivenParameters()
    {
        await this.Client.Rpc("echo", new Dictionary<string, object>
        {
            ["name"] = "supabot",
            ["count"] = 3,
            ["enabled"] = true
        });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

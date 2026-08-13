using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Supabase.Functions.Client;

namespace Functions.Tests;

/// <summary>
///     Pins the exact bytes an invocation puts on the wire. The client serializes the options' body bag and
///     always sends a JSON body (an empty object when none is supplied). This captures how nested values,
///     numbers, booleans and nulls are rendered — the transport contract the System.Text.Json migration must
///     preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class InvokeApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task InvokeRequest_ShouldSerializeAnEmptyObject_GivenNoBody()
    {
        await this.Client.Invoke("hello");
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task InvokeRequest_ShouldSerializeTheBody_GivenAPayload()
    {
        await this.Client.Invoke("hello", options: new InvokeFunctionOptions
        {
            Body = new Dictionary<string, object>
            {
                ["name"] = "supabase",
                ["count"] = 3,
                ["enabled"] = true,
                ["tags"] = new List<string> { "a", "b" },
                ["nested"] = new Dictionary<string, object> { ["key"] = "value" }
            }
        });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

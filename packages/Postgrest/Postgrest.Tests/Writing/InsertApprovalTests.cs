using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;

namespace Postgrest.Tests.Writing;

/// <summary>
///     Pins the exact bytes an insert puts on the wire. Postgrest serializes the model through the
///     state-aware <c>PostgrestContractResolver</c>, so this captures column-name mapping, per-column null
///     handling, enum mapping and — crucially — which columns the insert state drops (e.g. a primary key
///     whose <c>shouldInsert</c> is false). This is the transport contract the System.Text.Json migration
///     must preserve.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class InsertApprovalTests : RequestApprovalFixture
{
    [TestMethod]
    public async Task InsertRequest_ShouldSerializeToExpectedPayload_GivenTodo()
    {
        await this.Client.Table<Todo>().Insert(new Todo { Name = "walk the dog", Status = Todo.TodoStatus.IN_PROGRESS });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task InsertRequest_ShouldIncludeThePrimaryKey_GivenAModelWhosePrimaryKeyInserts()
    {
        await this.Client.Table<User>().Insert(new User { Username = "supabot", Status = "ONLINE" });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task InsertRequest_ShouldSerializeEveryColumnType_GivenKitchenSink()
    {
        await this.Client.Table<KitchenSink>().Insert(new KitchenSink
        {
            Id = new Guid("11111111-1111-1111-1111-111111111111"),
            StringValue = "text",
            BooleanValue = true,
            UniqueValue = "unique",
            IntValue = 42,
            LongValue = 9_000_000_000,
            FloatValue = 1.5f,
            DoubleValue = 2.5,
            DateTimeValue = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            ListOfStrings = new List<string> { "a", "b" },
            ListOfInts = new List<int> { 1, 2, 3 },
            ListOfFloats = new List<float> { 1.5f, 2.5f },
            IntRange = new IntRange(1, 10),
            Uuidv4 = new Guid("22222222-2222-2222-2222-222222222222")
        });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }

    [TestMethod]
    public async Task InsertRequest_ShouldSerializeToAJsonArray_GivenACollection()
    {
        await this.Client.Table<Todo>().Insert(new List<Todo>
        {
            new() { Name = "first", Status = Todo.TodoStatus.NOT_STARTED },
            new() { Name = "second", Status = Todo.TodoStatus.DONE }
        });
        await this.Verify(this.EmittedRequestBody).UseDirectory("Data");
    }
}

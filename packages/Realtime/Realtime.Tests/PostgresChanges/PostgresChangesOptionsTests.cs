using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace Realtime.Tests.PostgresChanges;

/// <summary>
///     The shape <see cref="PostgresChangesOptions" /> takes inside a channel's join
///     <c>config.postgres_changes</c> payload, plus the value-equality that lets a channel dedupe repeated
///     registrations of the same listener.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class PostgresChangesOptionsTests
{
    [TestMethod]
    public void Table_ShouldBeOmitted_GivenNull()
    {
        var json = JsonSerializer.Serialize(new PostgresChangesOptions("public"));
        JsonNode.Parse(json)!.AsObject().ContainsKey("table").Should().BeFalse();
    }

    [TestMethod]
    public void Table_ShouldBeSerialized_GivenProvided()
    {
        var json = JsonSerializer.Serialize(new PostgresChangesOptions("public", "todos"));
        JsonNode.Parse(json)!["table"]?.GetValue<string>().Should().Be("todos");
    }

    [TestMethod]
    public void Filter_ShouldBeOmitted_GivenNull()
    {
        var json = JsonSerializer.Serialize(new PostgresChangesOptions("public", "todos"));
        JsonNode.Parse(json)!.AsObject().ContainsKey("filter").Should().BeFalse();
    }

    [TestMethod]
    [DataRow(ListenType.All, "*")]
    [DataRow(ListenType.Inserts, "INSERT")]
    [DataRow(ListenType.Updates, "UPDATE")]
    [DataRow(ListenType.Deletes, "DELETE")]
    public void Event_ShouldRenderListenType(ListenType listenType, string expected) => new PostgresChangesOptions("public", "todos", listenType).Event.Should().Be(expected);

    [TestMethod]
    public void Equals_ShouldHold_GivenAllDiscriminatorsMatch()
    {
        var a = new PostgresChangesOptions("public", "todos", ListenType.Inserts, "id=eq.1");
        var b = new PostgresChangesOptions("public", "todos", ListenType.Inserts, "id=eq.1");
        using (new AssertionScope())
        {
            a.Equals(b).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }
    }

    [TestMethod]
    [DataRow("other", "todos", ListenType.Inserts, "id=eq.1")]
    [DataRow("public", "users", ListenType.Inserts, "id=eq.1")]
    [DataRow("public", "todos", ListenType.Updates, "id=eq.1")]
    [DataRow("public", "todos", ListenType.Inserts, "id=eq.2")]
    public void Equals_ShouldFail_GivenDiscriminatorDiffers(string schema, string table, ListenType listenType, string filter)
    {
        var reference = new PostgresChangesOptions("public", "todos", ListenType.Inserts, "id=eq.1");
        reference.Equals(new PostgresChangesOptions(schema, table, listenType, filter)).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_ShouldFail_GivenNull()
    {
        var reference = new PostgresChangesOptions("public", "todos");
        var result = reference.Equals((object?) null);
        result.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_ShouldFail_GivenDifferentType()
    {
        var reference = new PostgresChangesOptions("public", "todos");
        var result = reference.Equals(new object());
        result.Should().BeFalse();
    }
}

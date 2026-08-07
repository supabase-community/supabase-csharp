using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Support;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace Realtime.Tests.PostgresChanges;

/// <summary>
///     How a channel records a postgres_changes listener. <c>OnPostgresChange</c> is the one-call API: it
///     stores the options built from the filter and returns the channel so <c>Subscribe</c> can be chained.
///     The obsolete <c>Register</c> path feeds the same list.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class PostgresChangesRegistrationTests
{
    [TestMethod]
    public void OnPostgresChange_ShouldStoreOptionsAndReturnChannel()
    {
        var channel = Wire.Channel();
        var returned = channel.OnPostgresChange((_, _) => { }, ListenType.Inserts,
            new PostgresChangesFilter { Schema = "public", Table = "todos", Filter = "id=eq.1" });
        using (new AssertionScope())
        {
            returned.Should().BeSameAs(channel);
            channel.PostgresChangesOptions.Should().ContainSingle();
            var options = channel.PostgresChangesOptions[0];
            options.Schema.Should().Be("public");
            options.Table.Should().Be("todos");
            options.Filter.Should().Be("id=eq.1");
            options.Event.Should().Be("INSERT");
        }
    }

    [TestMethod]
    public void OnPostgresChange_ShouldDefaultToSchemaWideListener_GivenNoFilter()
    {
        var channel = Wire.Channel();
        channel.OnPostgresChange((_, _) => { }, ListenType.All);
        var options = channel.PostgresChangesOptions.Should().ContainSingle().Subject;
        options.Schema.Should().Be("public");
        options.Table.Should().BeNull();
    }

    [TestMethod]
    public void Register_ShouldRecordOptions()
    {
        var channel = Wire.Channel();
#pragma warning disable CS0618
        var returned = channel.Register(new PostgresChangesOptions("public", "todos"));
#pragma warning restore CS0618
        returned.Should().BeSameAs(channel);
        channel.PostgresChangesOptions.Should().ContainSingle().Which.Table.Should().Be("todos");
    }
}

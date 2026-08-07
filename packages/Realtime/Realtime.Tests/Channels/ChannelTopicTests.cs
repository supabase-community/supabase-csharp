using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Realtime;

namespace Realtime.Tests.Channels;

/// <summary>
///     The topic string a channel is keyed under: <see cref="Utils.GenerateChannelTopic" /> joins the
///     database/schema/table segments and appends a <c>col=eq.val</c> row filter, dropping empty segments.
///     Also covers the connection query string builder.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ChannelTopicTests
{
    [TestMethod]
    public void GenerateChannelTopic_ShouldJoinSegments()
    {
        Utils.GenerateChannelTopic("realtime", "public", "todos", null, null)
            .Should().Be("realtime:public:todos");
    }

    [TestMethod]
    public void GenerateChannelTopic_ShouldAppendRowFilter_GivenColumnAndValue()
    {
        Utils.GenerateChannelTopic("realtime", "public", "todos", "id", "1")
            .Should().Be("realtime:public:todos:id=eq.1");
    }

    [TestMethod]
    public void GenerateChannelTopic_ShouldDropEmptySegments()
    {
        Utils.GenerateChannelTopic("realtime", "", "", null, null).Should().Be("realtime");
    }

    [TestMethod]
    public void GenerateChannelTopic_ShouldIgnoreRowFilter_GivenMissingValue()
    {
        Utils.GenerateChannelTopic("realtime", "public", "todos", "id", null)
            .Should().Be("realtime:public:todos");
    }

    [TestMethod]
    public void QueryString_ShouldEmitProvidedPairsAndSkipEmpty()
    {
        var query = Utils.QueryString(new Dictionary<string, string?>
        {
            { "token", null },
            { "apikey", "anon-key" },
            { "vsn", "1.0.0" }
        });
        query.Should().Be("apikey=anon-key&vsn=1.0.0");
    }
}

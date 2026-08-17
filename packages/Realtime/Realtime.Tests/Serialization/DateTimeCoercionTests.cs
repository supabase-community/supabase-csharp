using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Realtime.Tests.Support;

namespace Realtime.Tests.Serialization;

/// <summary>
///     Date coercion the resolver wires onto <see cref="DateTime" /> columns: ISO timestamps round-trip, and
///     Postgres' <c>infinity</c>/<c>-infinity</c> sentinels map onto <see cref="DateTime.MaxValue" />/
///     <see cref="DateTime.MinValue" />. Driven through <see cref="CustomContractResolver" />, which is how the
///     converter is actually applied.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DateTimeCoercionTests
{
    private class DateModel
    {
        [JsonPropertyName("at")] public DateTime? At { get; set; }
        [JsonPropertyName("many")] public List<DateTime>? Many { get; set; }
    }

    private static DateModel Parse(string json) =>
        JsonSerializer.Deserialize<DateModel>(json, Wire.Settings())!;

    [TestMethod]
    public void At_ShouldParseIsoTimestamp() => Parse("{\"at\":\"2023-09-11T15:30:21Z\"}").At.Should().Be(new DateTime(2023, 9, 11, 15, 30, 21, DateTimeKind.Utc));

    [TestMethod]
    public void At_ShouldMapPositiveInfinityToMaxValue() => Parse("{\"at\":\"infinity\"}").At.Should().Be(DateTime.MaxValue);

    [TestMethod]
    public void At_ShouldMapNegativeInfinityToMinValue() => Parse("{\"at\":\"-infinity\"}").At.Should().Be(DateTime.MinValue);

    [TestMethod]
    public void Many_ShouldParseArrayOfTimestamps()
    {
        Parse("{\"many\":[\"2023-01-01T00:00:00Z\",\"2024-02-02T00:00:00Z\"]}")
            .Many.Should().HaveCount(2);
    }
}

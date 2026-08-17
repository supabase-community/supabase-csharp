using System;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Serialization;

/// <summary>
///     The serializer's timestamptz contract: reads preserve the exact instant and sub-second precision (and
///     never hand back an Unspecified kind), while writes of an unspecified-kind value are emitted verbatim
///     without being silently shifted to UTC.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DateTimeSerializationTests
{
    private static KitchenSink Deserialize(string json) =>
        JsonSerializer.Deserialize<KitchenSink>(json, Client.SerializerSettings())!;

    private static string Serialize(KitchenSink model) =>
        JsonSerializer.Serialize(model, Client.SerializerSettings());

    private static DateTime ParseUtc(string wireValue) =>
        DateTimeOffset.Parse(wireValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;

    [TestMethod]
    [DataRow("2024-06-26T18:30:45.1234560+00:00")]
    [DataRow("2024-06-26T20:30:45.1234560+02:00")]
    public void Read_ShouldPreserveTheInstant_GivenTimestamptzWithSubSeconds(string wireValue)
    {
        var model = Deserialize($"{{\"datetime_value\":\"{wireValue}\"}}");
        model.DateTimeValue!.Value.ToUniversalTime().Should().Be(ParseUtc(wireValue));
    }

    [TestMethod]
    [DataRow("2024-06-26T18:30:45.1234560+00:00")]
    [DataRow("2024-06-26T20:30:45.1234560+02:00")]
    public void Read_ShouldNotReturnUnspecifiedKind_GivenTimestamptz(string wireValue)
    {
        var model = Deserialize($"{{\"datetime_value\":\"{wireValue}\"}}");
        model.DateTimeValue!.Value.Kind.Should().NotBe(DateTimeKind.Unspecified);
    }

    [TestMethod]
    [DataRow("2024-06-26T18:30:45.1234560+00:00")]
    [DataRow("2024-06-26T20:30:45.1234560+02:00")]
    public void Read_ShouldPreserveEachInstant_GivenTimestamptzList(string wireValue)
    {
        var model = Deserialize($"{{\"list_of_datetimes\":[\"{wireValue}\"]}}");
        model.ListOfDateTimes.Should().ContainSingle()
            .Which.ToUniversalTime().Should().Be(ParseUtc(wireValue));
    }

    [TestMethod]
    public void Write_ShouldEmitUnspecifiedKindValueVerbatimWithoutUtcConversion()
    {
        var model = new KitchenSink { DateTimeValue = new DateTime(2026, 7, 8, 12, 0, 0) };
        Serialize(model).Should().Contain("\"datetime_value\":\"2026-07-08T12:00:00\"");
    }

    [TestMethod]
    public void Write_ShouldEmitUnspecifiedKindListVerbatimWithoutUtcConversion()
    {
        var model = new KitchenSink
        {
            ListOfDateTimes = [new DateTime(2021, 12, 10), new DateTime(2021, 12, 11), new DateTime(2021, 12, 12)]
        };
        Serialize(model).Should()
            .Contain("\"list_of_datetimes\":[\"2021-12-10T00:00:00\",\"2021-12-11T00:00:00\",\"2021-12-12T00:00:00\"]");
    }
}

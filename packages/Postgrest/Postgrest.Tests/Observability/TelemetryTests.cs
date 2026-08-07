using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Supabase.Postgrest.Constants;

namespace Postgrest.Tests.Observability;

/// <summary>
///     The diagnostics the SDK emits through <c>System.Diagnostics</c> (the "Supabase.Postgrest"
///     ActivitySource and Meter): an HTTP client span and a duration histogram, tagged per OpenTelemetry HTTP
///     conventions — and the sanitization rule that telemetry must never carry a query string or a filter value.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class TelemetryTests
{
    private const string SecretFilterValue = "secret-filter-value-42";

    private readonly List<Activity> activities = new();
    private readonly List<KeyValuePair<double, Dictionary<string, object?>>> measurements = new();
    private ActivityListener activityListener = null!;
    private MeterListener meterListener = null!;
    private WireMockServer server = null!;
    private Client client = null!;

    [Table("todos")]
    private class Todo : BaseModel
    {
        [PrimaryKey("id", false)] public int Id { get; set; }
        [Column("name")] public string? Name { get; set; }
    }

    [TestInitialize]
    public void SetUp()
    {
        server = WireMockServer.Start();
        client = new Client(server.Url!, new ClientOptions());
        activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == PostgrestDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(activityListener);
        meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == PostgrestDiagnostics.SourceName)
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
        {
            var tagValues = new Dictionary<string, object?>();
            foreach (var tag in tags)
                tagValues[tag.Key] = tag.Value;
            measurements.Add(new KeyValuePair<double, Dictionary<string, object?>>(value, tagValues));
        });
        meterListener.Start();
    }

    [TestCleanup]
    public void TearDown()
    {
        activityListener.Dispose();
        meterListener.Dispose();
        server.Stop();
    }

    [TestMethod]
    public async Task HttpSpan_ShouldRecordTheUrlWithoutItsQueryString()
    {
        MockTodosOk();
        await FilteredGet();
        SingleHttpSpan().GetTagItem("url.full").Should().Be($"{server.Url}/todos",
            "the query string carries column filters and their values and must never be recorded");
    }

    [TestMethod]
    public async Task HttpSpan_ShouldFollowOpenTelemetryConventionsAndTagTheOperation()
    {
        MockTodosOk();
        await FilteredGet();
        var span = SingleHttpSpan();
        span.Kind.Should().Be(ActivityKind.Client);
        span.GetTagItem("http.request.method").Should().Be("GET");
        span.GetTagItem("http.response.status_code").Should().Be(200);
        span.GetTagItem("db.operation").Should().Be("select");
    }

    [TestMethod]
    public async Task HttpSpan_ShouldBeMarkedAnError_GivenAFailedRequest()
    {
        server.Given(Request.Create().WithPath("/todos").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("{\"message\":\"boom\"}"));
        await ((Func<Task>) FilteredGet).Should().ThrowAsync<PostgrestException>();
        var span = SingleHttpSpan();
        span.Status.Should().Be(ActivityStatusCode.Error);
        span.GetTagItem("http.response.status_code").Should().Be(500);
    }

    [TestMethod]
    public async Task DurationHistogram_ShouldRecordOneMeasurementPerRequest()
    {
        MockTodosOk();
        await FilteredGet();
        meterListener.RecordObservableInstruments();
        var measurement = measurements.Should().ContainSingle().Subject;
        measurement.Key.Should().BeGreaterThan(0);
        measurement.Value["http.response.status_code"].Should().Be(200);
        measurement.Value["url.path"].Should().Be("/todos");
        measurement.Value["db.operation"].Should().Be("select");
    }

    [TestMethod]
    public async Task Telemetry_ShouldNeverContainAFilterValue()
    {
        MockTodosOk();
        await FilteredGet();
        var recorded = activities
            .SelectMany(a => a.TagObjects)
            .Select(tag => tag.Value?.ToString() ?? "")
            .Concat(measurements.SelectMany(m => m.Value.Values).Select(v => v?.ToString() ?? ""))
            .Concat(activities.Select(a => a.DisplayName));
        recorded.Should().NotContain(value => value.Contains(SecretFilterValue),
            "no span name, tag, or metric dimension may contain a column filter value");
    }

    private Task<Supabase.Postgrest.Responses.ModeledResponse<Todo>> FilteredGet() =>
        client.Table<Todo>().Filter("name", Operator.Equals, SecretFilterValue).Get();

    private void MockTodosOk() =>
        server.Given(Request.Create().WithPath("/todos").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

    private Activity SingleHttpSpan() =>
        activities.Single(a => a.OperationName == "GET /todos");
}

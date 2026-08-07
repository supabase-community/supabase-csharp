using System;
using System.Diagnostics;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Core.Diagnostics;

namespace Core.Tests.Diagnostics;

/// <summary>
/// Covers <see cref="ActivityExtensions"/>: the OpenTelemetry HTTP tags it writes onto a listened
/// <see cref="Activity"/>, the error status it raises for failing responses and exceptions, and its
/// no-op contract when nothing is listening (a null activity). URLs must reach tags only through
/// <see cref="UrlSanitizer"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ActivityExtensionsTests
{
    private readonly ActivitySource source = new("Core.Tests.ActivityExtensions");
    private readonly ActivityListener listener;

    public ActivityExtensionsTests()
    {
        this.listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate == this.source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(this.listener);
    }

    [TestCleanup]
    public void Cleanup()
    {
        this.listener.Dispose();
        this.source.Dispose();
    }

    private Activity StartActivity() => this.source.StartActivity("operation")!;

    [TestMethod]
    public void SetHttpRequestTags_ShouldTagMethodHostAndSanitizedUrl()
    {
        using var activity = this.StartActivity();
        activity.SetHttpRequestTags("POST", new Uri("https://project.supabase.co/auth/v1/token?apikey=secret"));
        using (new AssertionScope())
        {
            activity.GetTagItem("http.request.method").Should().Be("POST");
            activity.GetTagItem("server.address").Should().Be("project.supabase.co");
            activity.GetTagItem("url.full").Should().Be("https://project.supabase.co/auth/v1/token",
                "the query string may carry an api key and must never be tagged");
        }
    }

    [TestMethod]
    public void SetHttpRequestTags_ShouldTagPort_GivenNonDefaultPort()
    {
        using var activity = this.StartActivity();
        activity.SetHttpRequestTags("GET", new Uri("http://127.0.0.1:54321/auth/v1/token"));
        activity.GetTagItem("server.port").Should().Be(54321);
    }

    [TestMethod]
    public void SetHttpRequestTags_ShouldNotTagPort_GivenDefaultPort()
    {
        using var activity = this.StartActivity();
        activity.SetHttpRequestTags("GET", new Uri("https://project.supabase.co/auth/v1/token"));
        activity.GetTagItem("server.port").Should().BeNull();
    }

    [TestMethod]
    public void SetHttpRequestTags_ShouldReturnNull_GivenNullActivity() =>
        ((Activity?) null).SetHttpRequestTags("GET", new Uri("https://project.supabase.co")).Should().BeNull();

    [TestMethod]
    public void SetHttpResponseTags_ShouldTagStatusCode()
    {
        using var activity = this.StartActivity();
        activity.SetHttpResponseTags(200);
        using (new AssertionScope())
        {
            activity.GetTagItem("http.response.status_code").Should().Be(200);
            activity.Status.Should().Be(ActivityStatusCode.Unset);
        }
    }

    [TestMethod]
    public void SetHttpResponseTags_ShouldMarkError_GivenStatusAtErrorBoundary()
    {
        using var activity = this.StartActivity();
        activity.SetHttpResponseTags(400);
        using (new AssertionScope())
        {
            activity.GetTagItem("error.type").Should().Be("400");
            activity.Status.Should().Be(ActivityStatusCode.Error);
        }
    }

    [TestMethod]
    public void SetHttpResponseTags_ShouldNotMarkError_GivenLastSuccessStatus()
    {
        using var activity = this.StartActivity();
        activity.SetHttpResponseTags(399);
        using (new AssertionScope())
        {
            activity.GetTagItem("error.type").Should().BeNull();
            activity.Status.Should().Be(ActivityStatusCode.Unset);
        }
    }

    [TestMethod]
    public void SetHttpResponseTags_ShouldReturnNull_GivenNullActivity() =>
        ((Activity?) null).SetHttpResponseTags(500).Should().BeNull();

    [TestMethod]
    public void SetFailure_ShouldTagExceptionTypeAndErrorStatus()
    {
        using var activity = this.StartActivity();
        activity.SetFailure(new InvalidOperationException("boom"));
        using (new AssertionScope())
        {
            activity.GetTagItem("error.type").Should().Be(typeof(InvalidOperationException).FullName);
            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("boom");
        }
    }

    [TestMethod]
    public void SetFailure_ShouldReturnNull_GivenNullActivity() =>
        ((Activity?) null).SetFailure(new InvalidOperationException()).Should().BeNull();
}

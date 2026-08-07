using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Core.Diagnostics;

namespace Core.Tests.Diagnostics;

/// <summary>
/// Covers <see cref="UrlSanitizer"/>, which reduces a URL to <c>scheme://host[:port]/path</c> so no
/// secret carried in user info, the query string, or the fragment can reach telemetry or logs. Both
/// the <see cref="Uri"/> overload and the string overload (including its unparseable/relative
/// fallback) are exercised.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class UrlSanitizerTests
{
    [TestMethod]
    public void Sanitize_ShouldStripTheQueryString() =>
        UrlSanitizer.Sanitize(new Uri("https://project.supabase.co/auth/v1/token?grant_type=refresh_token&apikey=secret"))
            .Should().Be("https://project.supabase.co/auth/v1/token");

    [TestMethod]
    public void Sanitize_ShouldStripTheFragment() =>
        UrlSanitizer.Sanitize(new Uri("https://project.supabase.co/callback#access_token=secret-jwt"))
            .Should().Be("https://project.supabase.co/callback");

    [TestMethod]
    public void Sanitize_ShouldStripUserInfo() =>
        UrlSanitizer.Sanitize(new Uri("https://user:password@project.supabase.co/auth/v1/settings"))
            .Should().Be("https://project.supabase.co/auth/v1/settings");

    [TestMethod]
    public void Sanitize_ShouldKeepNonDefaultPorts() =>
        UrlSanitizer.Sanitize(new Uri("http://127.0.0.1:54321/auth/v1/token?grant_type=password"))
            .Should().Be("http://127.0.0.1:54321/auth/v1/token");

    [TestMethod]
    public void Sanitize_ShouldOmitDefaultPorts() =>
        UrlSanitizer.Sanitize(new Uri("https://project.supabase.co:443/auth/v1/settings"))
            .Should().Be("https://project.supabase.co/auth/v1/settings");

    [TestMethod]
    public void Sanitize_ShouldStripAfterPath_GivenRelativeUri() =>
        UrlSanitizer.Sanitize(new Uri("/auth/v1/verify?token=secret#fragment", UriKind.Relative))
            .Should().Be("/auth/v1/verify");

    [TestMethod]
    public void Sanitize_ShouldStripQueryAndFragment_GivenAbsoluteUrlString() =>
        UrlSanitizer.Sanitize("https://project.supabase.co/auth/v1/verify?token=secret#fragment")
            .Should().Be("https://project.supabase.co/auth/v1/verify");

    [TestMethod]
    public void Sanitize_ShouldStripQueryAndFragment_GivenUnparseableUrlString() =>
        UrlSanitizer.Sanitize("not a url?token=secret#fragment")
            .Should().Be("not a url");

    [TestMethod]
    public void Sanitize_ShouldReturnInputUnchanged_GivenStringWithoutQueryOrFragment() =>
        UrlSanitizer.Sanitize("not-a-url")
            .Should().Be("not-a-url");

    [TestMethod]
    public void Sanitize_ShouldReturnEmpty_GivenStringThatIsOnlyAQueryString() =>
        UrlSanitizer.Sanitize("?token=secret")
            .Should().BeEmpty("a delimiter at index 0 leaves no path to keep");
}

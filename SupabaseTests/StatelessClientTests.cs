using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Supabase.Gotrue.Constants;
using static Supabase.StatelessClient;

namespace SupabaseTests;

/// <summary>
/// The stateless facade owns the same composition concern as <see cref="Supabase.Client"/> — building
/// child-client options (headers, schema, urls) from a url/key — but expressed as pure static helpers.
/// All hermetic; the live stateless round-trip lives in <see cref="SupabaseClientDatabaseTests"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class StatelessClientTests
{
    private const string SupabaseUrl = "http://localhost";

    private static Supabase.SupabaseOptions FormatOptions() => new()
    {
        AuthUrlFormat = "{0}:54321/rest/v1",
        RealtimeUrlFormat = "ws://127.0.0.1:54321/realtime/v1",
        RestUrlFormat = "{0}:54321/rest/v1",
    };

    [TestMethod]
    public void GetAuthOptions_ShouldProduceUsableGotrueClient()
    {
        var gotrueOptions = GetAuthOptions(SupabaseUrl, null, FormatOptions());
        new Supabase.Gotrue.Client(gotrueOptions).SignIn(Provider.Spotify).Should().NotBeNull();
    }

    [TestMethod]
    public void GetAuthOptions_ShouldPreferDeveloperAuthorizationHeader_GivenAuthorizationInOptions()
    {
        var options = new Supabase.SupabaseOptions
        {
            AuthUrlFormat = "{0}:9999",
            RealtimeUrlFormat = "{0}:4000/socket",
            RestUrlFormat = "{0}:3000",
            Headers = new Dictionary<string, string> { { "Authorization", "Bearer 123" } }
        };
        GetAuthOptions(SupabaseUrl, "456", options).Headers.Should().ContainKey("Authorization")
            .WhoseValue.Should().Be("Bearer 123",
                "an explicit Authorization header must win over the key-derived bearer (issue #5)");
    }

    [TestMethod]
    public void GetRestOptions_ShouldComposeSchemaKeyAndAuthHeaders()
    {
        var restOptions = GetRestOptions("my-key", new Supabase.SupabaseOptions { Schema = "custom" });
        using (new AssertionScope())
        {
            restOptions.Schema.Should().Be("custom");
            restOptions.Headers.Should().ContainKey("apiKey").WhoseValue.Should().Be("my-key");
            restOptions.Headers.Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer my-key",
                "with no developer Authorization the bearer falls back to the supabase key");
            restOptions.Headers.Should().ContainKey("X-Client-Info");
        }
    }

    [TestMethod]
    public void GetRestOptions_ShouldMergeDeveloperSuppliedHeaders_GivenCustomHeader()
    {
        var options = new Supabase.SupabaseOptions();
        options.Headers["X-Custom"] = "custom-value";
        GetRestOptions("my-key", options).Headers.Should().ContainKey("X-Custom")
            .WhoseValue.Should().Be("custom-value", "developer headers must flow through to the child clients");
    }

    [TestMethod]
    public void Functions_ShouldReturnClient_GivenUrlAndKey()
    {
        Functions(SupabaseUrl, "my-key", FormatOptions()).Should().NotBeNull();
    }

    [TestMethod]
    public void Storage_ShouldReturnClient_GivenUrlAndKey()
    {
        Storage(SupabaseUrl, "my-key", FormatOptions()).Should().NotBeNull();
    }
}
using System;
using Supabase.Postgrest;

namespace Postgrest.Tests.Support;

/// <summary>
///     Shared entry point for the E2E tier: builds a <see cref="Client" /> pointed at the local Supabase CLI
///     stack (<c>supabase start</c>). The REST URL falls back to the CLI default, so the suite runs against a
///     fresh stack without any environment configuration, and honors <c>SUPABASE_URL</c> when a different
///     stack is targeted.
/// </summary>
internal static class LocalStack
{
    private const string DefaultUrl = "http://localhost:54321";

    internal static string RestUrl =>
        $"{Environment.GetEnvironmentVariable("SUPABASE_URL") ?? DefaultUrl}/rest/v1";

    internal static Client Client(ClientOptions? options = null) =>
        options is null ? new Client(RestUrl) : new Client(RestUrl, options);
}

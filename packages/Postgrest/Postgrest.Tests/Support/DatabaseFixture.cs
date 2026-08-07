using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace Postgrest.Tests.Support;

/// <summary>
///     Restores the local Supabase database to its seeded migration baseline before the whole suite runs, so
///     the E2E tier is idempotent across repeated runs. Those tests insert without per-test teardown; resetting
///     at assembly start rather than cleaning up at the end is deliberate — a run that crashes mid-way never
///     reaches an end-of-run cleanup, so each run instead heals whatever state the previous one left behind
///     (accumulated rows, or a value that poisons later reads). The reset truncates every table in the
///     <c>public</c>/<c>personal</c> schemas and replays the exact seed statements the CLI recorded for the
///     dummy_data migration, so there is no second copy of the seed to drift from the migration.
/// </summary>
[TestClass]
public static class DatabaseFixture
{
    // The dummy_data migration (supabase/migrations/1751173710_postgrest_dummy_data.sql). Its statements are
    // stored verbatim in supabase_migrations.schema_migrations, so we replay them instead of duplicating the
    // seed. The version is inlined below (not a parameter): Npgsql does not substitute placeholders inside a
    // dollar-quoted ($$...$$) block, and it is a hardcoded constant, so there is no injection surface.
    private const string ResetScript = """
        DO $$
        DECLARE r RECORD;
        BEGIN
            FOR r IN SELECT schemaname, tablename FROM pg_tables WHERE schemaname IN ('public', 'personal') LOOP
                EXECUTE format('TRUNCATE TABLE %I.%I RESTART IDENTITY CASCADE', r.schemaname, r.tablename);
            END LOOP;
        END $$;
        DO $$
        DECLARE stmt TEXT;
        BEGIN
            FOR stmt IN
                SELECT unnest(statements) FROM supabase_migrations.schema_migrations WHERE version = '1751173710'
            LOOP
                EXECUTE stmt;
            END LOOP;
        END $$;
        """;

    [AssemblyInitialize]
    public static async Task ResetToSeedBaseline(TestContext context)
    {
        _ = context;
        if (Environment.GetEnvironmentVariable("STRYKER_MUTATION") == "true")
            return; // mutation runs compile-exclude the E2E tier, so no database is required

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString());
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(ResetScript, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException exception)
        {
            // Hermetic-only environments (Unit/Contract with no stack up) have no database to reset; the E2E
            // tier fails loudly on its own if it actually needed one. Don't abort the whole assembly here.
            Console.Error.WriteLine($"[DatabaseFixture] Skipped seed reset: {exception.Message}");
        }
    }

    private static string ConnectionString()
    {
        var url = Environment.GetEnvironmentVariable("SUPABASE_DB_URL");
        if (string.IsNullOrWhiteSpace(url))
            return "Host=127.0.0.1;Port=54322;Username=postgres;Password=postgres;Database=postgres;Timeout=3";

        var uri = new Uri(url);
        var credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            Timeout = 3
        }.ConnectionString;
    }
}

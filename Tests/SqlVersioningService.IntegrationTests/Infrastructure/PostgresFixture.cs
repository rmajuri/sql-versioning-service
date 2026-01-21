using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SqlVersioningService.Infrastructure;
using Xunit;

namespace SqlVersioningService.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit collection fixture that manages Postgres database lifecycle for integration tests.
/// Schema is created once per test collection, not per test.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        "Host=localhost;Port=5432;Database=sql_versioning_test;Username=postgres;Password=postgres";

    private IConfiguration? _configuration;

    public async Task InitializeAsync()
    {
        // Build configuration for DatabaseContext
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Default"] = ConnectionString }
            )
            .Build();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Drop existing tables to ensure clean schema
        await conn.ExecuteAsync(
            @"
            DROP TABLE IF EXISTS query_versions CASCADE;
            DROP TABLE IF EXISTS queries CASCADE;
            DROP TABLE IF EXISTS sql_blobs CASCADE;
            DROP TABLE IF EXISTS api_keys CASCADE;
            DROP TABLE IF EXISTS ""QueryVersions"" CASCADE;
            DROP TABLE IF EXISTS ""Queries"" CASCADE;
        "
        );

        // Create schema once for the entire test collection
        await conn.ExecuteAsync(
            @"
            CREATE TABLE queries (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                head_version_id UUID NULL,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL,
                deleted_at TIMESTAMPTZ NULL
            );

            CREATE TABLE query_versions (
                id UUID PRIMARY KEY,
                query_id UUID NOT NULL REFERENCES queries(id) ON DELETE CASCADE,
                parent_version_id UUID NULL REFERENCES query_versions(id),
                blob_hash TEXT NOT NULL,
                note TEXT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE sql_blobs (
                hash TEXT PRIMARY KEY,
                bytes_size INT NOT NULL
            );

            CREATE TABLE api_keys (
                id UUID PRIMARY KEY,
                hashed_key TEXT NOT NULL UNIQUE,
                created_at TIMESTAMPTZ NOT NULL,
                revoked_at TIMESTAMPTZ NULL
            );
        "
        );
    }

    public async Task DisposeAsync()
    {
        // Optional: Clean up all data at the end of the test run
        // Individual tests handle their own cleanup via CleanupDatabase()
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a DatabaseContext for use in tests.
    /// Cleans up data before returning to ensure test isolation.
    /// </summary>
    public DatabaseContext CreateDatabaseContext()
    {
        CleanupDatabase();
        return new DatabaseContext(_configuration!);
    }

    /// <summary>
    /// Clears all data from tables while preserving schema.
    /// Called per test to ensure isolation.
    /// </summary>
    public void CleanupDatabase()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        // Order matters due to FK constraints
        conn.Execute(
            @"
            DELETE FROM query_versions;
            DELETE FROM queries;
            DELETE FROM sql_blobs;
            DELETE FROM api_keys;
        "
        );
    }
}

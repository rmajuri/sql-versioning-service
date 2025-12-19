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

        // Create schema once for the entire test collection
        await conn.ExecuteAsync(
            @"
            CREATE TABLE IF NOT EXISTS Queries (
                Id UUID PRIMARY KEY,
                Name TEXT NOT NULL,
                HeadVersionId UUID NULL,
                IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
                CreatedAt TIMESTAMPTZ NOT NULL,
                UpdatedAt TIMESTAMPTZ NOT NULL,
                DeletedAt TIMESTAMPTZ NULL
            );

            CREATE TABLE IF NOT EXISTS QueryVersions (
                Id UUID PRIMARY KEY,
                QueryId UUID NOT NULL REFERENCES Queries(Id) ON DELETE CASCADE,
                ParentVersionId UUID NULL REFERENCES QueryVersions(Id),
                BlobHash TEXT NOT NULL,
                Note TEXT NULL,
                CreatedAt TIMESTAMPTZ NOT NULL,
                UpdatedAt TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS sql_blobs (
                hash TEXT PRIMARY KEY,
                bytes_size INT NOT NULL
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
            DELETE FROM QueryVersions;
            DELETE FROM Queries;
            DELETE FROM sql_blobs;
        "
        );
    }
}

using System;
using System.Data;
using Dapper;
using Npgsql;
using SqlVersioningService.Infrastructure;

namespace SqlVersioningService.Tests.Infrastructure;

public static class TestDatabaseFactory
{
    // Use a dedicated test database
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sql_versioning_test;Username=postgres;Password=postgres";

    private static bool _initialized = false;
    private static readonly object _lock = new();

    public static DatabaseContext Create()
    {
        EnsureInitialized();
        CleanupDatabase();
        return new DatabaseContext(ConnectionString);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (_lock)
        {
            if (_initialized)
                return;

            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            // Ensure schema exists
            conn.Execute(
                @"
                CREATE TABLE IF NOT EXISTS Queries (
                    Id UUID PRIMARY KEY,
                    OrganizationId UUID NULL,
                    OwnerUserId UUID NULL,
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
                    AuthorId UUID NULL,
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

            _initialized = true;
        }
    }

    private static void CleanupDatabase()
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

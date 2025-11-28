using Dapper;
using SqlVersioningService.Models;
using SqlVersioningService.Infrastructure;

namespace SqlVersioningService.Repositories;

public class VersionRepository
{
    private readonly DatabaseContext _db;

    public VersionRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<QueryVersion?> GetLatestVersionAsync(int queryId)
    {
        const string sql = @"
            SELECT id, query_id AS QueryId, hash, created_at AS CreatedAt
            FROM query_versions
            WHERE query_id = @queryId
            ORDER BY created_at DESC
            LIMIT 1;
        ";

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<QueryVersion>(sql, new { queryId });
    }

    public async Task<IEnumerable<QueryVersion>> GetAllVersionsAsync(int queryId)
    {
        const string sql = @"
            SELECT id, query_id AS QueryId, hash, created_at AS CreatedAt
            FROM query_versions
            WHERE query_id = @queryId
            ORDER BY created_at DESC;
        ";

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<QueryVersion>(sql, new { queryId });
    }

    public async Task<int> InsertVersionAsync(int queryId, string hash)
    {
        const string sql = @"
            INSERT INTO query_versions (query_id, hash)
            VALUES (@queryId, @hash)
            RETURNING id;
        ";

        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { queryId, hash });
    }

    public async Task<bool> VersionExistsAsync(int queryId, string hash)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT 1 
                FROM query_versions
                WHERE query_id = @queryId AND hash = @hash
            );
        ";

        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { queryId, hash });
    }
}

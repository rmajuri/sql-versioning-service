using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class SqlBlobRepository
{
    private readonly DatabaseContext _db;

    public SqlBlobRepository(DatabaseContext db) => _db = db;

    private const string SqlSelectByHash =
        @"
        SELECT hash, bytes_size AS BytesSize
        FROM sql_blobs
        WHERE hash = @Hash;
    ";

    private const string SqlUpsert =
        @"
        INSERT INTO sql_blobs (hash, bytes_size)
        VALUES (@Hash, @BytesSize)
        ON CONFLICT (hash) DO NOTHING;
    ";

    private const string SqlExists =
        @"
        SELECT EXISTS(
            SELECT 1 FROM sql_blobs WHERE hash = @Hash
        );
    ";

    public async Task<SqlBlob?> GetByHashAsync(string hash)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SqlBlob>(SqlSelectByHash, new { Hash = hash });
    }

    public async Task<bool> CreateIfNotExistsAsync(SqlBlob blob)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(SqlUpsert, new { blob.Hash, blob.BytesSize });

        return affected > 0; // true = newly inserted, false = already existed
    }

    public async Task<bool> ExistsAsync(string hash)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(SqlExists, new { Hash = hash });
    }
}

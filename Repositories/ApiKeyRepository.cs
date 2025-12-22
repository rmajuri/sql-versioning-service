using Dapper;
using SqlVersioningService.Infrastructure;

namespace SqlVersioningService.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly DatabaseContext _db;

    public ApiKeyRepository(DatabaseContext db) => _db = db;

    private const string SqlIsValidHashedKey =
        @"
        SELECT EXISTS (
            SELECT 1
            FROM api_keys
            WHERE hashed_key = @HashedKey
              AND revoked_at IS NULL
        );
    ";

    private const string SqlInsert =
        @"
        INSERT INTO api_keys (id, hashed_key, created_at, revoked_at)
        VALUES (@Id, @HashedKey, @CreatedAt, NULL);
    ";

    public async Task<bool> IsValidHashedKeyAsync(string hashedKey)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            SqlIsValidHashedKey,
            new { HashedKey = hashedKey }
        );
    }

    public async Task InsertAsync(Guid id, string hashedKey, DateTimeOffset createdAt)
    {
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(
            SqlInsert,
            new
            {
                Id = id,
                HashedKey = hashedKey,
                CreatedAt = createdAt,
            }
        );
    }
}

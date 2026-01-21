using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class QueryRepository : IQueryRepository
{
    private readonly DatabaseContext _db;

    public QueryRepository(DatabaseContext db) => _db = db;

    // ------------------------------------------------------------
    // SQL STATEMENTS
    // ------------------------------------------------------------

    private const string SqlSelectById =
        @"
        SELECT id, name, head_version_id, is_deleted, created_at, updated_at, deleted_at
        FROM queries
        WHERE id = @Id AND is_deleted = FALSE;
    ";

    private const string SqlInsert =
        @"
        INSERT INTO queries
            (id, name, head_version_id, is_deleted, created_at, updated_at)
        VALUES
            (@Id, @Name, @HeadVersionId, @IsDeleted, @CreatedAt, @UpdatedAt);
    ";

    private const string SqlUpdate =
        @"
        UPDATE queries SET
            name = @Name,
            head_version_id = @HeadVersionId,
            updated_at = @UpdatedAt
        WHERE id = @Id;
    ";

    private const string SqlSoftDelete =
        @"
        UPDATE queries
        SET is_deleted = TRUE,
            deleted_at = @Now
        WHERE id = @Id;
    ";

    private const string SqlUpdateHeadVersion =
        @"
        UPDATE queries
        SET head_version_id = @VersionId,
            updated_at = @Now
        WHERE id = @QueryId;
    ";

    // ------------------------------------------------------------
    // REPOSITORY METHODS
    // ------------------------------------------------------------

    public async Task<Query?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Query>(SqlSelectById, new { Id = id });
    }

    public async Task CreateAsync(Query query)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            SqlInsert,
            new
            {
                query.Id,
                query.Name,
                query.HeadVersionId,
                query.IsDeleted,
                query.CreatedAt,
                query.UpdatedAt,
            }
        );
    }

    public async Task<bool> UpdateAsync(Query query)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            SqlUpdate,
            new
            {
                query.Name,
                query.HeadVersionId,
                query.UpdatedAt,
                query.Id,
            }
        );

        return affected > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            SqlSoftDelete,
            new { Id = id, Now = DateTimeOffset.UtcNow }
        );

        return affected > 0;
    }

    public async Task<bool> UpdateHeadVersionAsync(Guid queryId, Guid newVersionId)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            SqlUpdateHeadVersion,
            new
            {
                QueryId = queryId,
                VersionId = newVersionId,
                Now = DateTimeOffset.UtcNow,
            }
        );

        return affected > 0;
    }
}

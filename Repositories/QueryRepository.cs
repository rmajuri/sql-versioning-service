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
        SELECT Id, Name, HeadVersionId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
        FROM Queries
        WHERE Id = @Id AND IsDeleted = FALSE;
    ";

    private const string SqlInsert =
        @"
        INSERT INTO Queries
            (Id, Name, HeadVersionId, IsDeleted, CreatedAt, UpdatedAt)
        VALUES
            (@Id, @Name, @HeadVersionId, @IsDeleted, @CreatedAt, @UpdatedAt);
    ";

    private const string SqlUpdate =
        @"
        UPDATE Queries SET
            Name = @Name,
            HeadVersionId = @HeadVersionId,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
    ";

    private const string SqlSoftDelete =
        @"
        UPDATE Queries
        SET IsDeleted = TRUE,
            DeletedAt = @Now
        WHERE Id = @Id;
    ";

    private const string SqlUpdateHeadVersion =
        @"
        UPDATE Queries
        SET HeadVersionId = @VersionId,
            UpdatedAt = @Now
        WHERE Id = @QueryId;
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

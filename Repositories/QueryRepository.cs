using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class QueryRepository
{
    private readonly DatabaseContext _db;

    public QueryRepository(DatabaseContext db) => _db = db;

    // ------------------------------------------------------------
    // SQL STATEMENTS
    // ------------------------------------------------------------

    private const string SqlSelectById =
        @"
        SELECT Id, OrganizationId, OwnerUserId, Name, HeadVersionId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
        FROM Queries
        WHERE Id = @Id AND IsDeleted = FALSE;
    ";

    private const string SqlSelectByOrganizationId =
        @"
        SELECT Id, OrganizationId, OwnerUserId, Name, HeadVersionId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
        FROM Queries
        WHERE OrganizationId = @OrganizationId AND IsDeleted = FALSE;
    ";

    private const string SqlSelectByOwnerUserId =
        @"  
        SELECT Id, OrganizationId, OwnerUserId, Name, HeadVersionId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
        FROM Queries
        WHERE OwnerUserId = @OwnerUserId AND IsDeleted = FALSE; 
    ";

    private const string SqlInsert =
        @"
        INSERT INTO Queries
            (Id, OrganizationId, OwnerUserId, Name, HeadVersionId, IsDeleted, CreatedAt, UpdatedAt)
        VALUES
            (@Id, @OrganizationId, @OwnerUserId, @Name, @HeadVersionId, @IsDeleted, @CreatedAt, @UpdatedAt);
    ";

    private const string SqlUpdate =
        @"
        UPDATE Queries SET
            OrganizationId = @OrganizationId,
            OwnerUserId = @OwnerUserId,
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
                query.OrganizationId,
                query.OwnerUserId,
                query.Name,
                query.HeadVersionId,
                query.IsDeleted,
                query.CreatedAt,
                query.UpdatedAt,
            }
        );
    }

    public async Task<IEnumerable<Query>> GetByOrganizationIdAsync(Guid organizationId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Query>(
            SqlSelectByOrganizationId,
            new { OrganizationId = organizationId }
        );
    }

    public async Task<IEnumerable<Query>> GetByOwnerUserIdAsync(Guid ownerUserId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Query>(
            SqlSelectByOwnerUserId,
            new { OwnerUserId = ownerUserId }
        );
    }

    public async Task<bool> UpdateAsync(Query query)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            SqlUpdate,
            new
            {
                query.OrganizationId,
                query.OwnerUserId,
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
}

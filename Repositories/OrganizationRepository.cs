using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class OrganizationRepository
{
    private readonly DatabaseContext _db;

    public OrganizationRepository(DatabaseContext db) => _db = db;

    // ------------------------------------------------------------
    // SQL STATEMENTS
    // ------------------------------------------------------------

    private const string SqlSelectById =
        @"
        SELECT Id, Name, OrgAdminId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
        FROM Organizations
        WHERE Id = @Id AND IsDeleted = FALSE;
    ";

    private const string SqlInsert =
        @"
        INSERT INTO Organizations
            (Id, Name, OrgAdminId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt)
        VALUES
            (@Id, @Name, @OrgAdminId, @IsDeleted, @CreatedAt, @UpdatedAt, @DeletedAt);
    ";

    private const string SqlUpdate =
        @"
        UPDATE Organizations SET
            Name = @Name,
            OrgAdminId = @OrgAdminId,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
    ";

    private const string SqlSoftDelete =
        @"
        UPDATE Organizations SET
            IsDeleted = TRUE,
            DeletedAt = @Now,
            UpdatedAt = @Now
        WHERE Id = @Id;
    ";

    // ------------------------------------------------------------
    // REPOSITORY METHODS
    // ------------------------------------------------------------

    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Organization>(SqlSelectById, new { Id = id });
    }

    public async Task CreateAsync(Organization org)
    {
        if (org == null)
            throw new ArgumentNullException(nameof(org));

        if (org.Id == Guid.Empty)
            org.Id = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;
        if (org.CreatedAt == default)
            org.CreatedAt = now;
        org.UpdatedAt = now;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            SqlInsert,
            new
            {
                org.Id,
                org.Name,
                org.OrgAdminId,
                org.IsDeleted,
                org.CreatedAt,
                org.UpdatedAt,
                org.DeletedAt,
            }
        );
    }

    public async Task<bool> UpdateAsync(Organization org)
    {
        if (org == null)
            throw new ArgumentNullException(nameof(org));

        org.UpdatedAt = DateTimeOffset.UtcNow;

        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            SqlUpdate,
            new
            {
                org.Name,
                org.OrgAdminId,
                org.UpdatedAt,
                org.Id,
            }
        );

        return affected > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(SqlSoftDelete, new { Id = id, Now = DateTimeOffset.UtcNow });
        return affected > 0;
    }
}

using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class OrganizationMemberRepository
{
    private readonly DatabaseContext _db;

    public OrganizationMemberRepository(DatabaseContext db) => _db = db;

    // ------------------------------------------------------------
    // SQL STATEMENTS
    // ------------------------------------------------------------

    private const string SqlSelectByUserId =
        @"
        SELECT OrganizationId, UserId, Role, IsDeleted, JoinedAt, UpdatedAt, DeletedAt
        FROM OrganizationMembers
        WHERE UserId = @UserId AND IsDeleted = FALSE;
    ";

    private const string SqlSelectByOrganizationId =
        @"
        SELECT OrganizationId, UserId, Role, IsDeleted, JoinedAt, UpdatedAt, DeletedAt
        FROM OrganizationMembers
        WHERE OrganizationId = @OrganizationId AND IsDeleted = FALSE;
    ";

    private const string SqlInsert =
        @"
        INSERT INTO OrganizationMembers
            (OrganizationId, UserId, Role, IsDeleted, JoinedAt, UpdatedAt, DeletedAt)
        VALUES
            (@OrganizationId, @UserId, @Role, @IsDeleted, @JoinedAt, @UpdatedAt, @DeletedAt);
    ";

    private const string SqlUpdate =
        @"
        UPDATE OrganizationMembers SET
            Role = @Role,
            UpdatedAt = @UpdatedAt
        WHERE OrganizationId = @OrganizationId AND UserId = @UserId;
    ";

    private const string SqlSoftDelete =
        @"
        UPDATE OrganizationMembers SET
            IsDeleted = TRUE,
            DeletedAt = @Now,
            UpdatedAt = @Now
        WHERE OrganizationId = @OrganizationId AND UserId = @UserId;
    ";

    // ------------------------------------------------------------
    // REPOSITORY METHODS
    // ------------------------------------------------------------

    public async Task<IEnumerable<OrganizationMember>> GetByUserIdAsync(Guid userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<OrganizationMember>(SqlSelectByUserId, new { UserId = userId });
    }

    public async Task<IEnumerable<OrganizationMember>> GetByOrganizationIdAsync(Guid organizationId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<OrganizationMember>(SqlSelectByOrganizationId, new { OrganizationId = organizationId });
    }

    public async Task CreateAsync(OrganizationMember member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));

        var now = DateTimeOffset.UtcNow;
        if (member.JoinedAt == default)
            member.JoinedAt = now;
        member.UpdatedAt = now;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            SqlInsert,
            new
            {
                member.OrganizationId,
                member.UserId,
                member.Role,
                member.IsDeleted,
                member.JoinedAt,
                member.UpdatedAt,
                member.DeletedAt,
            }
        );
    }

    public async Task<bool> UpdateAsync(OrganizationMember member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));

        member.UpdatedAt = DateTimeOffset.UtcNow;

        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            SqlUpdate,
            new
            {
                Role = member.Role,
                UpdatedAt = member.UpdatedAt,
                OrganizationId = member.OrganizationId,
                UserId = member.UserId,
            }
        );

        return affected > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid organizationId, Guid userId)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(SqlSoftDelete, new { OrganizationId = organizationId, UserId = userId, Now = DateTimeOffset.UtcNow });
        return affected > 0;
    }
}

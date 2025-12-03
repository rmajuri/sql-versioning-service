using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class UserRepository
{
    private readonly DatabaseContext _db;

    public UserRepository(DatabaseContext db) => _db = db;

    // ------------------------------------------------------------
    // SQL STATEMENTS
    // ------------------------------------------------------------

    private const string SqlSelectById =
        @"
        SELECT Id, Email, Name, CreatedAt, UpdatedAt, DeletedAt
        FROM Users
        WHERE Id = @Id AND IsDeleted = FALSE;
    ";

    private const string SqlSelectByEmail =
        @"
        SELECT Id, Email, Name, CreatedAt, UpdatedAt, DeletedAt
        FROM Users
        WHERE Email = @Email AND IsDeleted = FALSE;
    ";

    private const string SqlEmailExists =
        @"
        SELECT EXISTS(
            SELECT 1 FROM Users WHERE Email = @Email AND IsDeleted = FALSE
        );
    ";

    private const string SqlInsert =
        @"
        INSERT INTO Users (Id, Email, Name, CreatedAt, UpdatedAt, DeletedAt)
        VALUES (@Id, @Email, @Name, @CreatedAt, @UpdatedAt, @DeletedAt);
    ";

    private const string SqlUpdate =
        @"
        UPDATE Users SET
            Email = @Email,
            Name = @Name,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
    ";

    private const string SqlSoftDelete =
        @"
        UPDATE Users SET
            DeletedAt = @Now,
            UpdatedAt = @Now
        WHERE Id = @Id;
    ";

    private const string SqlSelectMembershipsByUserId =
        @"
        SELECT OrganizationId, UserId, Role, JoinedAt, UpdatedAt
        FROM OrganizationMembers
        WHERE UserId = @UserId AND IsDeleted = FALSE;
    ";

    // ------------------------------------------------------------
    // REPOSITORY METHODS
    // ------------------------------------------------------------

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(SqlSelectById, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(SqlSelectByEmail, new { Email = email });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(SqlEmailExists, new { Email = email });
    }

    public async Task<Guid> CreateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        if (user.CreatedAt == default)
            user.CreatedAt = now;
        user.UpdatedAt = now;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            SqlInsert,
            new
            {
                user.Id,
                user.Email,
                user.Name,
                user.CreatedAt,
                user.UpdatedAt,
                user.DeletedAt,
            }
        );

        return user.Id;
    }

    public async Task UpdateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        user.UpdatedAt = DateTimeOffset.UtcNow;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            SqlUpdate,
            new
            {
                user.Email,
                user.Name,
                user.UpdatedAt,
                user.Id,
            }
        );
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(SqlSoftDelete, new { Id = id, Now = DateTimeOffset.UtcNow });
    }

    public async Task<User?> GetUserWithMembershipsAsync(Guid id)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QuerySingleOrDefaultAsync<User>(SqlSelectById, new { Id = id });
        if (user == null)
            return null;

        var memberships = (
            await conn.QueryAsync<OrganizationMember>(
                SqlSelectMembershipsByUserId,
                new { UserId = id }
            )
        ).ToList();
        // set the back-reference to the loaded user
        foreach (var m in memberships)
        {
            m.User = user;
        }

        user.OrganizationMemberships = memberships;
        return user;
    }
}

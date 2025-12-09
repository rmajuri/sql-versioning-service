using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class VersionRepository
{
    private readonly DatabaseContext _db;

    public VersionRepository(DatabaseContext db)
    {
        _db = db;
    }

    // ------------------------------------------------------------
    // SQL STATEMENTS
    // ------------------------------------------------------------

    private const string SqlSelectLatestByQueryId =
        @"
        SELECT Id, QueryId, ParentVersionId, AuthorId, BlobHash, Note, CreatedAt, UpdatedAt
        FROM QueryVersions
        WHERE QueryId = @QueryId
        ORDER BY CreatedAt DESC
        LIMIT 1;
    ";

    private const string SqlSelectByQueryId =
        @"
        SELECT Id, QueryId, ParentVersionId, AuthorId, BlobHash, Note, CreatedAt, UpdatedAt
        FROM QueryVersions
        WHERE QueryId = @QueryId
        ORDER BY CreatedAt DESC;
    ";

    private const string SqlInsert =
        @"
        INSERT INTO QueryVersions
            (Id, QueryId, ParentVersionId, AuthorId, BlobHash, Note, CreatedAt, UpdatedAt)
        VALUES
            (@Id, @QueryId, @ParentVersionId, @AuthorId, @BlobHash, @Note, @CreatedAt, @UpdatedAt);
    ";

    private const string SqlExists =
        @"
        SELECT EXISTS(
            SELECT 1 FROM QueryVersions WHERE QueryId = @QueryId AND BlobHash = @BlobHash
        );
    ";

    private const string SqlSelectHeadVersionId =
        @"
        SELECT HeadVersionId
        FROM Queries
        WHERE Id = @QueryId;
    ";

    // ------------------------------------------------------------
    // REPOSITORY METHODS
    // ------------------------------------------------------------

    public async Task<QueryVersion?> GetLatestVersionAsync(Guid queryId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<QueryVersion>(
            SqlSelectLatestByQueryId,
            new { QueryId = queryId }
        );
    }

    public async Task<IEnumerable<QueryVersion>> GetAllVersionsAsync(Guid queryId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<QueryVersion>(SqlSelectByQueryId, new { QueryId = queryId });
    }

    public async Task CreateAsync(QueryVersion version)
    {
        if (version == null)
            throw new ArgumentNullException(nameof(version));

        // ensure ids and timestamps
        if (version.Id == Guid.Empty)
            version.Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        if (version.CreatedAt == default)
            version.CreatedAt = now;
        version.UpdatedAt = now;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            SqlInsert,
            new
            {
                version.Id,
                version.QueryId,
                version.ParentVersionId,
                version.AuthorId,
                version.BlobHash,
                version.Note,
                version.CreatedAt,
                version.UpdatedAt,
            }
        );
    }

    public async Task<bool> VersionExistsAsync(Guid queryId, string blobHash)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            SqlExists,
            new { QueryId = queryId, BlobHash = blobHash }
        );
    }

    public async Task<Guid?> GetHeadVersionIdAsync(Guid queryId)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<Guid?>(SqlSelectHeadVersionId, new { QueryId = queryId });
    }

}

using Dapper;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public class VersionRepository : IVersionRepository
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
        SELECT id, query_id, parent_version_id, blob_hash, note, created_at, updated_at
        FROM query_versions
        WHERE query_id = @QueryId
        ORDER BY created_at DESC
        LIMIT 1;
    ";

    private const string SqlSelectByQueryId =
        @"
        SELECT id, query_id, parent_version_id, blob_hash, note, created_at, updated_at
        FROM query_versions
        WHERE query_id = @QueryId
        ORDER BY created_at DESC;
    ";

    private const string SqlInsert =
        @"
        INSERT INTO query_versions
            (id, query_id, parent_version_id, blob_hash, note, created_at, updated_at)
        VALUES
            (@Id, @QueryId, @ParentVersionId, @BlobHash, @Note, @CreatedAt, @UpdatedAt);
    ";

    private const string SqlExists =
        @"
        SELECT EXISTS(
            SELECT 1 FROM query_versions WHERE query_id = @QueryId AND blob_hash = @BlobHash
        );
    ";

    private const string SqlSelectHeadVersionId =
        @"
        SELECT head_version_id
        FROM queries
        WHERE id = @QueryId;
    ";

    private const string SqlSelectById =
        @"
        SELECT id, query_id, parent_version_id, blob_hash, note, created_at, updated_at
        FROM query_versions
        WHERE id = @VersionId
        LIMIT 1;
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

    public async Task<QueryVersion?> GetByIdAsync(Guid versionId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<QueryVersion>(
            SqlSelectById,
            new { VersionId = versionId }
        );
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
        return await conn.ExecuteScalarAsync<Guid?>(
            SqlSelectHeadVersionId,
            new { QueryId = queryId }
        );
    }
}

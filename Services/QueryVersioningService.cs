using SqlVersioningService.Models;
using SqlVersioningService.Repositories;

namespace SqlVersioningService.Services;

public class QueryVersioningService : IQueryVersioningService
{
    private readonly IQueryRepository _queryRepo;
    private readonly IVersionRepository _versionRepo;
    private readonly ISqlBlobRepository _blobRepo;
    private readonly IBlobStorageService _blobStorage;
    private readonly IHashingService _hashingService;

    public QueryVersioningService(
        IQueryRepository queryRepo,
        IVersionRepository versionRepo,
        ISqlBlobRepository blobRepo,
        IBlobStorageService blobStorage,
        IHashingService hashingService
    )
    {
        _queryRepo = queryRepo;
        _versionRepo = versionRepo;
        _blobRepo = blobRepo;
        _blobStorage = blobStorage;
        _hashingService = hashingService;
    }

    public async Task<QueryVersion> CreateVersionAsync(Guid queryId, string sql, string? note)
    {
        var now = DateTimeOffset.UtcNow;

        // 1. Compute hash
        var hash = _hashingService.ComputeHash(sql);

        // 2. Upload blob if needed
        if (!await _blobRepo.ExistsAsync(hash))
        {
            await _blobStorage.UploadAsync(hash, sql);

            var blob = new SqlBlob { Hash = hash, BytesSize = sql.Length };

            await _blobRepo.CreateIfNotExistsAsync(blob);
        }

        // 3. Get parent version id
        var parentVersionId = await _versionRepo.GetHeadVersionIdAsync(queryId);

        // 4. Insert version
        var version = new QueryVersion
        {
            Id = Guid.NewGuid(),
            QueryId = queryId,
            BlobHash = hash,
            ParentVersionId = parentVersionId,
            Note = note,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _versionRepo.CreateAsync(version);

        // 5. Update Query Head pointer
        await _queryRepo.UpdateHeadVersionAsync(queryId, version.Id);

        return version;
    }

    public async Task<IEnumerable<QueryVersion>> GetVersionsForQueryAsync(Guid queryId)
    {
        return await _versionRepo.GetAllVersionsAsync(queryId);
    }

    public async Task<QueryVersion?> GetVersionByIdAsync(Guid versionId)
    {
        return await _versionRepo.GetByIdAsync(versionId);
    }

    public async Task<string?> GetSqlForVersionAsync(Guid versionId)
    {
        var version = await _versionRepo.GetByIdAsync(versionId);
        if (version == null)
            return null;

        return await _blobStorage.DownloadAsync(version.BlobHash);
    }
}

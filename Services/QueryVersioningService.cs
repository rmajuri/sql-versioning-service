using SqlVersioningService.Models;
using SqlVersioningService.Repositories;

namespace SqlVersioningService.Services;

public class QueryVersioningService
{
    private readonly QueryRepository _queryRepo;
    private readonly VersionRepository _versionRepo;
    private readonly SqlBlobRepository _blobRepo;
    private readonly IBlobStorageService _blobStorage;
    private readonly HashingService _hashingService;

    public QueryVersioningService(
        QueryRepository queryRepo,
        VersionRepository versionRepo,
        SqlBlobRepository blobRepo,
        IBlobStorageService blobStorage,
        HashingService hashingService
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
}

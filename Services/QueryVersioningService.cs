using SqlVersioningService.Repositories;
using SqlVersioningService.Models;

namespace SqlVersioningService.Services;

public class QueryVersioningService
{
    private readonly QueryRepository _queryRepo;
    private readonly VersionRepository _versionRepo;
    private readonly HashingService _hashingService;
    private readonly IBlobStorageService _blobStorageService;

    public QueryVersioningService(
        QueryRepository queryRepo,
        VersionRepository versionRepo,
        HashingService hashingService,
        IBlobStorageService blobStorageService
    )
    {
        _queryRepo = queryRepo;
        _versionRepo = versionRepo;
        _hashingService = hashingService;
        _blobStorageService = blobStorageService;
    }

    public async Task<QueryVersion> CreateVersionAsync(
        Guid queryId,
        string sql,
        string? note)
    {
        // 1. Compute hash
        var hash = _hashingService.ComputeHash(sql);

        // 2. Create version record
        var version = new QueryVersion
        {
            Id = Guid.NewGuid(),
            QueryId = queryId,
            BlobHash = hash,
            Note = note,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _versionRepo.CreateAsync(version);

        // 3. Update Query's HeadVersionId
        var query = await _queryRepo.GetByIdAsync(queryId) ?? throw new Exception("Query not found");
        query.HeadVersionId = version.Id;
        await _queryRepo.UpdateAsync(query);

        return version;
    }
}

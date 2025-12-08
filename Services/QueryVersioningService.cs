using SqlVersioningService.Repositories;

namespace SqlVersioningService.Services;

public class QueryVersioningService
{
    private readonly QueryRepository _queryRepo;
    private readonly VersionRepository _versionRepo;
    private readonly HashingService _hashingService;

    public QueryVersioningService(
        QueryRepository _queryRepo,
        VersionRepository _versionRepo,
        HashingService _hashingService
    )
    {
        _queryRepo = _queryRepo;
        _versionRepo = _versionRepo;
        _hashingService = _hashingService;
    }

    public async Task<string> ComputeVersionHashAsync(string sql)
    {
        return _hashing.ComputeHash(sql);
    }
}

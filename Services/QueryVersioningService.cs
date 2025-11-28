using SqlVersioningService.Repositories;

namespace SqlVersioningService.Services;

public class QueryVersioningService
{
    private readonly QueryRepository _queries;
    private readonly VersionRepository _versions;
    private readonly HashingService _hashing;

    public QueryVersioningService(
        QueryRepository queries,
        VersionRepository versions,
        HashingService hashing)
    {
        _queries = queries;
        _versions = versions;
        _hashing = hashing;
    }

    public async Task<string> ComputeVersionHashAsync(string sql)
    {
        return _hashing.ComputeHash(sql);
    }
}

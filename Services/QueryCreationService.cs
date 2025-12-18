using SqlVersioningService.DTOs.Responses;
using SqlVersioningService.Models;

namespace SqlVersioningService.Services;

public class QueryCreationService
{
    private readonly QueryService _queryService;
    private readonly QueryVersioningService _versionService;

    public QueryCreationService(QueryService queryService, QueryVersioningService versionService)
    {
        _queryService = queryService;
        _versionService = versionService;
    }

    public async Task<QueryWithVersionResponse> CreateQueryAsync(
        string name,
        string sql,
        string? note
    )
    {
        // 1. Create the Query record
        var query = new Query
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        await _queryService.CreateAsync(query);

        // 2. Create initial version
        var version = await _versionService.CreateVersionAsync(query.Id, sql, note);

        // 3. Return DTO
        return new QueryWithVersionResponse(query, version);
    }
}

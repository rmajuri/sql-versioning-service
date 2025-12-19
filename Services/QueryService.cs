using SqlVersioningService.Models;
using SqlVersioningService.Repositories;

namespace SqlVersioningService.Services;

public class QueryService : IQueryService
{
    private readonly IQueryRepository _queryRepo;

    public QueryService(IQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public async Task<Query?> GetByIdAsync(Guid id)
    {
        return await _queryRepo.GetByIdAsync(id);
    }

    public async Task<Guid> CreateAsync(Query query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        if (query.Id == Guid.Empty)
            query.Id = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;
        if (query.CreatedAt == default)
            query.CreatedAt = now;
        query.UpdatedAt = now;

        await _queryRepo.CreateAsync(query);
        return query.Id;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        return await _queryRepo.SoftDeleteAsync(id);
    }
}

using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public interface IQueryRepository
{
    Task<Query?> GetByIdAsync(Guid id);
    Task CreateAsync(Query query);
    Task<bool> UpdateAsync(Query query);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> UpdateHeadVersionAsync(Guid queryId, Guid newVersionId);
}

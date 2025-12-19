using SqlVersioningService.Models;

namespace SqlVersioningService.Services;

public interface IQueryService
{
    Task<Query?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(Query query);
    Task<bool> SoftDeleteAsync(Guid id);
}

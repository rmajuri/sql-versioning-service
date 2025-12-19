using SqlVersioningService.Models;

namespace SqlVersioningService.Services;

public interface IQueryVersioningService
{
    Task<QueryVersion> CreateVersionAsync(Guid queryId, string sql, string? note);
}

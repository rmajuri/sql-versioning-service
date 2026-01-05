using SqlVersioningService.Models;

namespace SqlVersioningService.Services;

public interface IQueryVersioningService
{
    Task<QueryVersion> CreateVersionAsync(Guid queryId, string sql, string? note);
    Task<IEnumerable<QueryVersion>> GetVersionsForQueryAsync(Guid queryId);
    Task<QueryVersion?> GetVersionByIdAsync(Guid versionId);
    Task<string?> GetSqlForVersionAsync(Guid versionId);
}

using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public interface IVersionRepository
{
    Task<QueryVersion?> GetLatestVersionAsync(Guid queryId);
    Task<IEnumerable<QueryVersion>> GetAllVersionsAsync(Guid queryId);
    Task CreateAsync(QueryVersion version);
    Task<bool> VersionExistsAsync(Guid queryId, string blobHash);
    Task<Guid?> GetHeadVersionIdAsync(Guid queryId);
}

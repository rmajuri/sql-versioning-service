using SqlVersioningService.Models;

namespace SqlVersioningService.Repositories;

public interface ISqlBlobRepository
{
    Task<SqlBlob?> GetByHashAsync(string hash);
    Task<bool> CreateIfNotExistsAsync(SqlBlob blob);
    Task<bool> ExistsAsync(string hash);
}

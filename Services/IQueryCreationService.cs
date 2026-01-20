using System.Threading.Tasks;
using SqlVersioningService.DTOs.Responses;

namespace SqlVersioningService.Services
{
    public interface IQueryCreationService
    {
        Task<QueryWithVersionResponse> CreateQueryAsync(string name, string sql, string? note);
    }
}

using SqlVersioningService.Models;
using SqlVersioningService.Infrastructure;
using Dapper;

namespace SqlVersioningService.Repositories;

public class QueryRepository
{
    private readonly DatabaseContext _db;

    public QueryRepository(DatabaseContext db) => _db = db;

    public async Task<Query?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Query>(
            "SELECT * FROM queries WHERE id = @id", new { id });
    }
}

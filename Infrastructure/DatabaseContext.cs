using Npgsql;

namespace SqlVersioningService.Infrastructure;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}

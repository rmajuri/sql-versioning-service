using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;

static string Sha256(string s)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

string connStr = Environment.GetEnvironmentVariable("DB_CONN")
    ?? throw new Exception("DB_CONN env var is required");

string migrationsDir = Environment.GetEnvironmentVariable("MIGRATIONS_DIR")
    ?? "/app/ops/migrate";

if (!Directory.Exists(migrationsDir))
    throw new Exception($"Migrations dir not found: {migrationsDir}");

var files = Directory.GetFiles(migrationsDir, "*.sql")
    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine($"Found {files.Count} migrations in {migrationsDir}");

await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();

await conn.ExecuteAsync("""
CREATE TABLE IF NOT EXISTS schema_migrations (
  id TEXT PRIMARY KEY,
  checksum TEXT NOT NULL,
  applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
""");

// Read applied migrations
var applied = (await conn.QueryAsync<string>("SELECT id FROM schema_migrations"))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

foreach (var path in files)
{
    var id = Path.GetFileName(path); // e.g. 001_core_domain_tables.sql
    if (applied.Contains(id))
    {
        Console.WriteLine($"SKIP {id}");
        continue;
    }

    var sql = await File.ReadAllTextAsync(path);
    var checksum = Sha256(sql);

    Console.WriteLine($"APPLY {id}");

    // Apply each file in a transaction (all-or-nothing)
    await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);
    try
    {
        await conn.ExecuteAsync(sql, transaction: tx);

        await conn.ExecuteAsync(
            "INSERT INTO schema_migrations (id, checksum) VALUES (@id, @checksum)",
            new { id, checksum },
            transaction: tx);

        await tx.CommitAsync();
        Console.WriteLine($"DONE  {id}");
    }
    catch
    {
        await tx.RollbackAsync();
        Console.WriteLine($"FAIL  {id}");
        throw;
    }
}

Console.WriteLine("Migrations complete.");

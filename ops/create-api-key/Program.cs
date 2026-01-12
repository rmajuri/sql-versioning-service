using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;

// ============================================================
// Operator Script: API Key Generator
// ============================================================
// This script generates a new API key, hashes it, stores the hash
// in the database, and prints the plaintext key once to stdout.
//
// Usage:
//   dotnet run -- <connection_string>
//
// Example:
//   dotnet run -- "Host=localhost;Port=5432;Database=sql_versioning;Username=postgres;Password=postgres"
//
// The plaintext API key is printed ONCE. Store it securely.
// ============================================================

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: dotnet run -- <connection_string>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine(
        "  dotnet run -- \"Host=localhost;Port=5432;Database=sql_versioning;Username=postgres;Password=postgres\""
    );
    Environment.Exit(1);
}

var connectionString = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("DB_CONN");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Usage: dotnet run -- <connection_string>");
    Console.Error.WriteLine("Or set DB_CONN env var.");
    Environment.Exit(1);
}

try
{
    // Generate a cryptographically secure random key (32 bytes = 256 bits)
    var keyBytes = RandomNumberGenerator.GetBytes(32);
    var plaintextKey = Convert.ToBase64String(keyBytes);

    // Hash the key using SHA256 (same algorithm as HashingService)
    var hashedKey = ComputeHash(plaintextKey);

    // Create the API key record
    var id = Guid.NewGuid();
    var createdAt = DateTimeOffset.UtcNow;

    // Insert into database
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    const string sql =
        @"
        INSERT INTO api_keys (id, hashed_key, created_at, revoked_at)
        VALUES (@Id, @HashedKey, @CreatedAt, NULL);
    ";

    await connection.ExecuteAsync(
        sql,
        new
        {
            Id = id,
            HashedKey = hashedKey,
            CreatedAt = createdAt,
        }
    );

    // Print the plaintext key to stdout (only time it's ever displayed)
    Console.WriteLine();
    Console.WriteLine("=== API KEY CREATED SUCCESSFULLY ===");
    Console.WriteLine();
    Console.WriteLine($"Key ID: {id}");
    Console.WriteLine($"Created: {createdAt:O}");
    Console.WriteLine();
    Console.WriteLine("API Key (store this securely - it will NOT be shown again):");
    Console.WriteLine();
    Console.WriteLine(plaintextKey);
    Console.WriteLine();
    Console.WriteLine("=====================================");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

// Same hashing algorithm as SqlVersioningService.Services.HashingService
static string ComputeHash(string input)
{
    using var sha = SHA256.Create();
    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(hash);
}

namespace SqlVersioningService.Repositories;

public interface IApiKeyRepository
{
    /// <summary>
    /// Checks if the given hashed key exists and is not revoked.
    /// </summary>
    /// <param name="hashedKey">The SHA256 hash of the API key</param>
    /// <returns>True if the key exists and is valid (not revoked)</returns>
    Task<bool> IsValidHashedKeyAsync(string hashedKey);

    /// <summary>
    /// Inserts a new API key record into the database.
    /// </summary>
    /// <param name="id">Unique identifier for the key</param>
    /// <param name="hashedKey">The SHA256 hash of the API key</param>
    /// <param name="createdAt">Timestamp when the key was created</param>
    Task InsertAsync(Guid id, string hashedKey, DateTimeOffset createdAt);
}

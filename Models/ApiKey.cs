namespace SqlVersioningService.Models;

public class ApiKey
{
    public Guid Id { get; set; }

    public string HashedKey { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}

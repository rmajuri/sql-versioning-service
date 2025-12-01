using System;

namespace SqlVersioningService.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

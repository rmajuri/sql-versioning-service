using System;

namespace SqlVersioningService.Models;

public class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid OrgAdminId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

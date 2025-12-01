using System;

namespace SqlVersioningService.Models;

public class Query
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? HeadVersionId { get; set; }

    public Boolean IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

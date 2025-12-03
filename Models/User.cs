using System;
using System.Collections.Generic;

namespace SqlVersioningService.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }
    public Boolean IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    // populated by repository when requested
    public List<OrganizationMember>? OrganizationMemberships { get; set; }
}

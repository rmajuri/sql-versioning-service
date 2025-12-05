using System;

namespace SqlVersioningService.Models.Responses;

public class OrganizationResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid OrgAdminId { get; set; }


    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

}

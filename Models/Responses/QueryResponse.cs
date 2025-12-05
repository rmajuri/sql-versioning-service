using System;

namespace SqlVersioningService.Models.Responses;

public class QueryResponse
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? HeadVersionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

}

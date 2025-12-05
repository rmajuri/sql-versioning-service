using System;

namespace SqlVersioningService.Models.Responses;

public class QueryVersionResponse
{
    public Guid Id { get; set; }

    public Guid QueryId { get; set; }

    public Guid? ParentVersionId { get; set; }

    public Guid AuthorId { get; set; }

    public string BlobHash { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

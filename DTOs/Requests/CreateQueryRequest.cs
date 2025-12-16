using SqlVersioningService.Models;

namespace SqlVersioningService.DTOs.Responses;

public record CreateQueryRequest(
    string Name,
    Guid OrganizationId,
    Guid OwnerUserId,
    string Sql,
    string? Note
);
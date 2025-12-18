using SqlVersioningService.Models;

namespace SqlVersioningService.DTOs.Responses;

public record CreateQueryRequest(string Name, string Sql, string? Note);

namespace SqlVersioningService.DTOs.Requests;

public record CreateVersionRequest(string Sql, string? Note = null);

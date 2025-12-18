using SqlVersioningService.Models;

namespace SqlVersioningService.DTOs.Responses;

public record QueryWithVersionResponse(Query Query, QueryVersion Version);

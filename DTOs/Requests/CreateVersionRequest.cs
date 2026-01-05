using System.ComponentModel.DataAnnotations;

namespace SqlVersioningService.DTOs.Requests;

public sealed record CreateVersionRequest
{
    [Required]
    [MinLength(1)]
    public required string Sql { get; init; }

    public string? Note { get; init; }
}

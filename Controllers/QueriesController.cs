using Microsoft.AspNetCore.Mvc;
using SqlVersioningService.Services;

namespace SqlVersioningService.Controllers;

[ApiController]
[Route("queries")]
public class QueriesController : ControllerBase
{
    private readonly QueryService _queryService;
    private readonly QueryCreationService _creationService;

    public QueriesController(
        QueryService queryService,
        QueryCreationService creationService
    )
    {
        _queryService = queryService;
        _creationService = creationService;
    }

    // ------------------------------------------------------------
    // CREATE QUERY + INITIAL VERSION
    // ------------------------------------------------------------

    public record CreateQueryRequest(
        string Name,
        Guid OrganizationId,
        Guid OwnerUserId,
        string Sql,
        string? Note
    );

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQueryRequest req)
    {
        var result = await _creationService.CreateQueryAsync(
            req.Name,
            req.OrganizationId,
            req.OwnerUserId,
            req.Sql,
            req.Note
        );

        return Ok(result);
    }

    // ------------------------------------------------------------
    // GET QUERY BY ID
    // ------------------------------------------------------------

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = await _queryService.GetByIdAsync(id);
        if (query == null)
            return NotFound();

        return Ok(query);
    }
}

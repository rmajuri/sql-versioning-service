using Microsoft.AspNetCore.Mvc;
using SqlVersioningService.DTOs.Requests;
using SqlVersioningService.Services;

namespace SqlVersioningService.Controllers;

[ApiController]
[Route("")]
public class VersionsController : ControllerBase
{
    private readonly IQueryVersioningService _versionService;

    public VersionsController(IQueryVersioningService versionService)
    {
        _versionService = versionService;
    }

    // ------------------------------------------------------------
    // CREATE NEW VERSION FOR A QUERY
    // ------------------------------------------------------------

    [HttpPost("queries/{queryId:guid}/versions")]
    public async Task<IActionResult> CreateVersion(
        Guid queryId,
        [FromBody] CreateVersionRequest req
    )
    {
        if (string.IsNullOrWhiteSpace(req.Sql))
            return BadRequest(new { error = "Sql is required." });

        var version = await _versionService.CreateVersionAsync(queryId, req.Sql, req.Note);

        return CreatedAtAction(nameof(GetVersionById), new { versionId = version.Id }, version);
    }

    // ------------------------------------------------------------
    // LIST ALL VERSIONS FOR A QUERY
    // ------------------------------------------------------------

    [HttpGet("queries/{queryId:guid}/versions")]
    public async Task<IActionResult> GetVersionsForQuery(Guid queryId)
    {
        var versions = await _versionService.GetVersionsForQueryAsync(queryId);
        return Ok(versions);
    }

    // ------------------------------------------------------------
    // GET VERSION METADATA BY ID
    // ------------------------------------------------------------

    [HttpGet("versions/{versionId:guid}")]
    public async Task<IActionResult> GetVersionById(Guid versionId)
    {
        var version = await _versionService.GetVersionByIdAsync(versionId);

        if (version == null)
            return NotFound();

        return Ok(version);
    }

    // ------------------------------------------------------------
    // GET SQL CONTENT FOR A VERSION
    // ------------------------------------------------------------

    [HttpGet("versions/{versionId:guid}/sql")]
    public async Task<IActionResult> GetSqlForVersion(Guid versionId)
    {
        var sql = await _versionService.GetSqlForVersionAsync(versionId);
        return sql is null ? NotFound() : Ok(new { sql });
    }
}

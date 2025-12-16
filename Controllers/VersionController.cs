using Microsoft.AspNetCore.Mvc;
using SqlVersioningService.DTOs.Requests;
using SqlVersioningService.DTOs.Responses;
using SqlVersioningService.Repositories;
using SqlVersioningService.Services;

namespace SqlVersioningService.Controllers;

[ApiController]
[Route("")]
public class VersionsController : ControllerBase
{
    private readonly QueryVersioningService _versionService;
    private readonly VersionRepository _versionRepo;
    private readonly IBlobStorageService _blobStorage;

    public VersionsController(
        QueryVersioningService versionService,
        VersionRepository versionRepo,
        IBlobStorageService blobStorage
    )
    {
        _versionService = versionService;
        _versionRepo = versionRepo;
        _blobStorage = blobStorage;
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
        var version = await _versionService.CreateVersionAsync(queryId, req.Sql, req.Note);

        return Ok(version);
    }

    // ------------------------------------------------------------
    // LIST ALL VERSIONS FOR A QUERY
    // ------------------------------------------------------------

    [HttpGet("queries/{queryId:guid}/versions")]
    public async Task<IActionResult> GetVersionsForQuery(Guid queryId)
    {
        var versions = await _versionRepo.GetAllVersionsAsync(queryId);
        return Ok(versions);
    }

    // ------------------------------------------------------------
    // GET VERSION METADATA BY ID
    // ------------------------------------------------------------

    [HttpGet("versions/{versionId:guid}")]
    public async Task<IActionResult> GetVersionById(Guid versionId)
    {
        var versions = await _versionRepo.GetAllVersionsAsync(Guid.Empty);
        var version = versions.FirstOrDefault(v => v.Id == versionId);

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
        var versions = await _versionRepo.GetAllVersionsAsync(Guid.Empty);
        var version = versions.FirstOrDefault(v => v.Id == versionId);

        if (version == null)
            return NotFound();

        var sql = await _blobStorage.DownloadAsync(version.BlobHash);
        return Ok(new { sql });
    }
}

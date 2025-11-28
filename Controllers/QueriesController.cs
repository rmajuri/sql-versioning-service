using Microsoft.AspNetCore.Mvc;
using SqlVersioningService.Services;
using SqlVersioningService.Models.Requests;

namespace SqlVersioningService.Controllers;

[ApiController]
[Route("queries")]
public class QueriesController : ControllerBase
{
    private readonly QueryVersioningService _svc;

    public QueriesController(QueryVersioningService svc) => _svc = svc;

    [HttpPost("hash")]
    public async Task<IActionResult> HashQuery([FromBody] CreateQueryRequest req)
    {
        var hash = await _svc.ComputeVersionHashAsync(req.Sql);
        return Ok(new { hash });
    }
}

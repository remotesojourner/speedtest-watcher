using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Web.Api;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/speedtests")]
public class SpeedtestsController : ControllerBase
{
    private readonly ResultsService _results;
    private readonly StatisticsService _statistics;
    private readonly PauseService _pause;
    private readonly SpeedtestRunService _runs;

    public SpeedtestsController(ResultsService results, StatisticsService statistics, PauseService pause, SpeedtestRunService runs)
    {
        _results = results;
        _statistics = statistics;
        _pause = pause;
        _runs = runs;
    }

    [HttpGet]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> ListTests(
        [FromQuery] int? afterId,
        [FromQuery] int limit = 10,
        [FromQuery] TestStatus? status = null,
        [FromQuery] TestType? type = null,
        [FromQuery] bool? healthy = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _results.ListAsync(afterId, limit, status, type, healthy, cancellationToken));

    [HttpGet("statistics")]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> GetStatistics([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? tz, CancellationToken cancellationToken) =>
        (await _statistics.GetAsync(from, to, tz, cancellationToken)).ToActionResult(statistics => Ok(statistics.ToDto()));

    [HttpGet("count")]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> CountTests(
        [FromQuery] TestStatus? status = null, [FromQuery] TestType? type = null, [FromQuery] bool? healthy = null, CancellationToken cancellationToken = default) =>
        Ok(new { count = await _results.CountAsync(status, type, healthy, cancellationToken) });

    [HttpPost("export")]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> ExportTests([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var file = await _results.ExportAsync(request, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("status")]
    [Authorize(Policy = AccessPolicies.Read)]
    public IActionResult GetStatus() => Ok(_pause.GetStatus());

    [HttpPost("run")]
    public async Task<IActionResult> RunTest([FromQuery] int? serverId, CancellationToken cancellationToken) =>
        (await _runs.StartManualRunAsync(serverId?.ToString(CultureInfo.InvariantCulture), cancellationToken)).ToActionResult("Speedtest successfully created");

    [HttpPost("pause")]
    public IActionResult Pause([FromBody] PauseRequest? request) =>
        _pause.Pause(request?.ResumeIn).ToActionResult("Successfully paused the speedtests");

    [HttpPost("continue")]
    public IActionResult Continue() => _pause.Resume().ToActionResult("Successfully resumed the speedtests");

    [HttpGet("{id:int}")]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        (await _results.GetAsync(id, cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        (await _results.DeleteAsync(id, cancellationToken)).ToActionResult("Successfully deleted the provided speedtest");
}

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Background;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/speedtests")]
public class SpeedtestsController : ControllerBase
{
    private readonly ISpeedtestRepository _repository;
    private readonly IPauseStateService _pauseState;
    private readonly SpeedtestSchedulerService _scheduler;
    private readonly IConfigRepository _configRepo;

    public SpeedtestsController(
        ISpeedtestRepository repository,
        IPauseStateService pauseState,
        SpeedtestSchedulerService scheduler,
        IConfigRepository configRepo)
    {
        _repository = repository;
        _pauseState = pauseState;
        _scheduler = scheduler;
        _configRepo = configRepo;
    }

    [HttpGet]
    public async Task<IActionResult> ListTests(
        [FromQuery] int? afterId,
        [FromQuery] int limit = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? healthy = null)
    {
        var tests = await _repository.ListTestsAsync(afterId, limit, status, type, healthy);
        return Ok(tests.Select(SpeedtestDto.From));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string? from, [FromQuery] string? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var toDate = to ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var stats = await _repository.GetStatisticsAsync(fromDate, toDate);
        return Ok(stats);
    }

    [HttpGet("count")]
    public async Task<IActionResult> CountTests([FromQuery] string? status = null, [FromQuery] string? type = null, [FromQuery] bool? healthy = null)
    {
        return Ok(new { count = await _repository.CountMatchingAsync(status, type, healthy) });
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportTests([FromBody] ExportRequest request)
    {
        var tests = await _repository.ListMatchingAsync(request.Status, request.Type, request.Healthy, request.Ids);

        return request.Format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? File(Encoding.UTF8.GetBytes(SpeedtestExport.ToJson(tests)), "application/json", "speedtests.json")
            : File(Encoding.UTF8.GetBytes(SpeedtestExport.ToCsv(tests)), "text/csv", "speedtests.csv");
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new StatusDto
        {
            Paused = _pauseState.IsPaused,
            Running = _pauseState.IsRunning
        });
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunTest([FromQuery] int? serverId)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        if (_pauseState.IsRunning)
            return Conflict(new { message = "Speedtest is already running" });

        var provider = await _configRepo.GetValueAsync("provider");
        if (provider == null || provider == "none")
            return StatusCode(StatusCodes.Status410Gone, new { message = "No speedtest provider selected" });

        if (_pauseState.IsPaused)
            return StatusCode(StatusCodes.Status410Gone, new { message = "Speedtests are paused" });

        _ = Task.Run(async () => await _scheduler.ExecuteSpeedtestAsync(
            "custom", serverOverride: serverId?.ToString(CultureInfo.InvariantCulture)));
        return Ok(new { message = "Speedtest successfully created" });
    }

    [HttpPost("pause")]
    public IActionResult Pause([FromBody] PauseRequest? request)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        _pauseState.Pause(request?.ResumeIn);
        return Ok(new { message = "Successfully paused the speedtests" });
    }

    [HttpPost("continue")]
    public IActionResult Continue()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        _pauseState.Resume();
        return Ok(new { message = "Successfully resumed the speedtests" });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var test = await _repository.GetByIdAsync(id);
        if (test == null)
            return NotFound(new { message = "Speedtest not found" });

        return Ok(SpeedtestDto.From(test));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        var deleted = await _repository.DeleteByIdAsync(id);
        if (!deleted)
            return NotFound(new { message = "Speedtest not found" });

        return Ok(new { message = "Successfully deleted the provided speedtest" });
    }
}

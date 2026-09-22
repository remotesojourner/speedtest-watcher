using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Web.Api;
using SpeedtestWatcher.Web.Api.Contracts;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/speedtests")]
[Tags("Speedtests")]
[Produces("application/json")]
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

    /// <summary>
    /// List results
    /// </summary>
    /// <remarks>
    /// Returns results newest first. To get the next page, pass the <c>id</c> of the last result you received as <c>afterId</c>. An <c>afterId</c> that doesn't exist gives an empty list.
    /// </remarks>
    /// <param name="afterId">Continue after this result.</param>
    /// <param name="limit">How many results to return. 0 or less gives 10.</param>
    /// <param name="status">Only results with this status.</param>
    /// <param name="type">Only results started this way: <c>auto</c> by the schedule, <c>custom</c> by hand.</param>
    /// <param name="healthy">Only results that met (<c>true</c>) or missed (<c>false</c>) their targets.</param>
    /// <response code="200">The results.</response>
    /// <response code="400">A filter has a value that isn't allowed.</response>
    [HttpGet]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<IReadOnlyList<SpeedtestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SpeedtestResponse>>> ListTests(
        [FromQuery] int? afterId,
        [FromQuery] int limit = 10,
        [FromQuery] TestStatus? status = null,
        [FromQuery] TestType? type = null,
        [FromQuery] bool? healthy = null,
        CancellationToken cancellationToken = default) =>
        Ok((await _results.ListAsync(afterId, limit, status, type, healthy, cancellationToken)).Select(SpeedtestResponse.From).ToList());

    /// <summary>
    /// Get a result
    /// </summary>
    /// <param name="id">The result's id.</param>
    /// <response code="200">The result.</response>
    /// <response code="404">No result has this id.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<SpeedtestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpeedtestResponse>> GetById(int id, CancellationToken cancellationToken) =>
        (await _results.GetAsync(id, cancellationToken)).ToActionResult(SpeedtestResponse.From);

    /// <summary>
    /// Delete a result
    /// </summary>
    /// <param name="id">The result's id.</param>
    /// <response code="200">The result was deleted.</response>
    /// <response code="404">No result has this id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> Delete(int id, CancellationToken cancellationToken) =>
        (await _results.DeleteAsync(id, cancellationToken)).ToActionResult("Successfully deleted the provided speedtest");

    /// <summary>
    /// Get statistics
    /// </summary>
    /// <remarks>
    /// Summarises the tests in a period: counts, the lowest, average and highest readings, how steady they were, averages per hour of the day, and points for charts.
    ///
    /// <c>from</c> and <c>to</c> each take a date (<c>2026-09-14</c>), which covers that whole day, or a date and time. A time without an offset is read in <c>tz</c>. A value that can't be read falls back to the default.
    /// </remarks>
    /// <param name="from">The start of the period. Defaults to seven days ago.</param>
    /// <param name="to">The end of the period. Defaults to today.</param>
    /// <param name="tz">An IANA time zone, such as <c>Europe/London</c>, for day boundaries and the hourly averages. Defaults to UTC.</param>
    /// <response code="200">The statistics.</response>
    /// <response code="400">The time zone isn't one the server knows.</response>
    [HttpGet("statistics")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<StatisticsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? tz, CancellationToken cancellationToken) =>
        (await _statistics.GetAsync(from, to, tz, cancellationToken)).ToActionResult(StatisticsResponse.From);

    /// <summary>
    /// Export results
    /// </summary>
    /// <remarks>
    /// Downloads the matching results as a CSV or JSON file, oldest first. A JSON export can be imported again with <c>PUT /api/storage/tests/history</c>.
    /// </remarks>
    /// <param name="request">Which results to export and in what format.</param>
    /// <response code="200">The file.</response>
    /// <response code="400">The request body isn't valid.</response>
    [HttpPost("export")]
    [Consumes("application/json")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "text/csv", "application/json")]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest, "application/json")]
    public async Task<IActionResult> ExportTests([FromBody] ExportResultsRequest request, CancellationToken cancellationToken) =>
        (await _results.ExportAsync(request.ToExportRequest(), cancellationToken)).ToFileResult();

    /// <summary>
    /// Get the run status
    /// </summary>
    /// <response code="200">Whether a test is running and whether tests are paused.</response>
    [HttpGet("status")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<RunStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<RunStatusResponse> GetStatus() => RunStatusResponse.From(_pause.GetStatus());

    /// <summary>
    /// Start a speedtest
    /// </summary>
    /// <remarks>
    /// Starts a manual test and answers straight away, while the test runs in the background. Poll <c>GET /api/speedtests/status</c> or the result list to see when it has finished.
    /// </remarks>
    /// <param name="serverId">Test against this server instead of the configured choice. Only providers with server choice use it.</param>
    /// <response code="200">The test has started.</response>
    /// <response code="400">The server id isn't a number.</response>
    /// <response code="409">A test is already running, tests are paused, or no provider is chosen.</response>
    [HttpPost("run")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>> RunTest([FromQuery] int? serverId, CancellationToken cancellationToken) =>
        (await _runs.StartManualRunAsync(serverId?.ToString(CultureInfo.InvariantCulture), cancellationToken)).ToActionResult("Speedtest successfully created");

    /// <summary>
    /// Pause speedtests
    /// </summary>
    /// <remarks>
    /// Stops scheduled and manual tests. A test that is already running finishes. The pause is kept in memory, so restarting the app resumes tests.
    /// </remarks>
    /// <param name="request">How long to pause. Send no body, or leave <c>resumeIn</c> out, to pause until tests are resumed.</param>
    /// <response code="200">Tests are paused.</response>
    /// <response code="400">The pause is longer than 720 hours.</response>
    [HttpPost("pause")]
    [Consumes("application/json")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<MessageResponse> Pause([FromBody] PauseRequest? request) =>
        _pause.Pause(request?.ResumeIn).ToActionResult("Successfully paused the speedtests");

    /// <summary>
    /// Resume speedtests
    /// </summary>
    /// <response code="200">Tests are running on schedule again.</response>
    [HttpPost("continue")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public ActionResult<MessageResponse> Continue() => _pause.Resume().ToActionResult("Successfully resumed the speedtests");
}

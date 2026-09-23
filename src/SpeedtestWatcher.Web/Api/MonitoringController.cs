using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Web.Api.Contracts;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.Web.Api;

[ApiController]
[Route("api/monitoring")]
[Tags("Monitoring")]
[Produces("application/json")]
public class MonitoringController : ControllerBase
{
    private readonly MonitoringService _monitoring;

    public MonitoringController(MonitoringService monitoring)
    {
        _monitoring = monitoring;
    }

    /// <summary>
    /// Get the connection state
    /// </summary>
    /// <remarks>
    /// The monitor opens a TCP connection to each probe target every interval. The line counts as down after the configured number of failed rounds, and up again after the configured number of good ones.
    /// </remarks>
    /// <response code="200">The current state and uptime for the last 24 hours, 7 days and 30 days.</response>
    [HttpGet("status")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<MonitoringStatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MonitoringStatusResponse>> GetStatus(CancellationToken cancellationToken) =>
        Ok(MonitoringStatusResponse.From(await _monitoring.StatusAsync(cancellationToken)));

    /// <summary>
    /// List outages
    /// </summary>
    /// <param name="limit">How many outages to return, newest first. At most 200.</param>
    /// <response code="200">The outages of the last year that are still stored.</response>
    [HttpGet("outages")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<IReadOnlyList<OutageResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OutageResponse>>> ListOutages([FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        Ok((await _monitoring.OutagesAsync(limit, cancellationToken)).Select(OutageResponse.From).ToList());

    /// <summary>
    /// Get the uptime calendar
    /// </summary>
    /// <remarks>
    /// One entry for each of the last 365 days, oldest first, for the calendar on the Uptime page.
    /// </remarks>
    /// <param name="tz">An IANA time zone, such as <c>Europe/London</c>, for day boundaries. Defaults to UTC.</param>
    /// <response code="200">The days.</response>
    [HttpGet("days")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<IReadOnlyList<UptimeDayResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UptimeDayResponse>>> ListDays([FromQuery] string? tz, CancellationToken cancellationToken) =>
        Ok((await _monitoring.DaysAsync(tz, cancellationToken)).Select(UptimeDayResponse.From).ToList());

    /// <summary>
    /// Delete an outage
    /// </summary>
    /// <remarks>
    /// For planned maintenance you don't want counted against your uptime.
    /// </remarks>
    /// <param name="id">The outage's id.</param>
    /// <response code="200">The outage was deleted.</response>
    /// <response code="404">No outage has this id.</response>
    [HttpDelete("outages/{id:int}")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> DeleteOutage(int id, CancellationToken cancellationToken) =>
        (await _monitoring.DeleteOutageAsync(id, cancellationToken)).ToActionResult("Successfully deleted the provided outage");
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.Web.Api;

[ApiController]
[Route("api/prometheus")]
[Tags("Prometheus")]
public class PrometheusController : ControllerBase
{
    private readonly ResultsService _results;
    private readonly MonitoringService _monitoring;

    public PrometheusController(ResultsService results, MonitoringService monitoring)
    {
        _results = results;
        _monitoring = monitoring;
    }

    /// <summary>
    /// Get Prometheus metrics
    /// </summary>
    /// <remarks>
    /// The latest readings in the Prometheus text format, each prefixed with <c>speedtest_watcher_</c>. Readings come from the latest completed test, so a failed or skipped test leaves no gaps. <c>last_test_timestamp_seconds</c> shows when any test last ran and <c>last_completed_test_timestamp_seconds</c> when one last completed.
    /// </remarks>
    /// <response code="200">The metrics.</response>
    [HttpGet("metrics")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/plain")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken) =>
        Content(
            PrometheusMetrics.Format(
                await _results.SummarizeAsync(cancellationToken),
                await _results.DataUsedAsync(cancellationToken),
                await _monitoring.StatusAsync(cancellationToken)),
            PrometheusMetrics.ContentType);
}

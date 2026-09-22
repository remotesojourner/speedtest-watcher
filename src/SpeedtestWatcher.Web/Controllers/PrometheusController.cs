using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Api;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/prometheus")]
public class PrometheusController : ControllerBase
{
    private readonly ResultsService _results;

    public PrometheusController(ResultsService results)
    {
        _results = results;
    }

    [HttpGet("metrics")]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken) =>
        Content(PrometheusMetrics.Format(await _results.SummarizeAsync(cancellationToken)), PrometheusMetrics.ContentType);
}

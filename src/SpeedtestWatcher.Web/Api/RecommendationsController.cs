using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Api;

[ApiController]
[Route("api/recommendations")]
[Tags("Recommendations")]
[Produces("application/json")]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService _recommendations;

    public RecommendationsController(RecommendationService recommendations)
    {
        _recommendations = recommendations;
    }

    /// <summary>
    /// Get the recommended targets
    /// </summary>
    /// <remarks>
    /// The best ping, download and upload of the last 10 completed tests, updated after every completed test.
    /// </remarks>
    /// <response code="200">The recommended targets.</response>
    /// <response code="404">Fewer than 10 tests have completed.</response>
    [HttpGet]
    [ProducesResponseType<RecommendationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationResponse>> Get(CancellationToken cancellationToken) =>
        (await _recommendations.GetAsync(cancellationToken)).ToActionResult(RecommendationResponse.From);
}

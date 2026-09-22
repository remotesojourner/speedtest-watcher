using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService _recommendations;

    public RecommendationsController(RecommendationService recommendations)
    {
        _recommendations = recommendations;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await _recommendations.GetAsync(cancellationToken)).ToActionResult();
}

using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationRepository _repository;

    public RecommendationsController(IRecommendationRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        bool isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var rec = await _repository.GetAsync();
        if (rec == null) return NotFound(new { message = "No recommendations found" });

        return Ok(rec);
    }
}

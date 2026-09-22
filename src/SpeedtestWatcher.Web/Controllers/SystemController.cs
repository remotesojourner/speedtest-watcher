using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Updates;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/info")]
[Tags("Info")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    private readonly VersionService _versions;

    public SystemController(VersionService versions)
    {
        _versions = versions;
    }

    /// <summary>
    /// Get the version
    /// </summary>
    /// <remarks>
    /// Returns this instance's version and the latest release on GitHub. The release is checked at most every six hours, or every hour after a failed check.
    /// </remarks>
    /// <response code="200">Both versions.</response>
    [HttpGet("version")]
    [ProducesResponseType<VersionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VersionResponse>> GetVersion(CancellationToken cancellationToken) =>
        VersionResponse.From(await _versions.GetVersionAsync(cancellationToken));
}

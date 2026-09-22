using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Info;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/info")]
public class SystemController : ControllerBase
{
    private readonly SystemInfoService _info;

    public SystemController(SystemInfoService info)
    {
        _info = info;
    }

    [HttpGet("version")]
    public async Task<IActionResult> GetVersion(CancellationToken cancellationToken) => Ok(await _info.GetVersionAsync(cancellationToken));

    [HttpGet("server/{provider}")]
    public async Task<IActionResult> GetServers(string provider, CancellationToken cancellationToken) =>
        (await _info.GetServersAsync(provider, cancellationToken)).ToActionResult();

    [HttpGet("interfaces")]
    public async Task<IActionResult> GetInterfaces(CancellationToken cancellationToken) => Ok(await _info.GetInterfacesAsync(cancellationToken));
}

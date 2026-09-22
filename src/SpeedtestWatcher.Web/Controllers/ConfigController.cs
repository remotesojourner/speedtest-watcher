using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Web.Api;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly SettingsService _settings;

    public ConfigController(SettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    [Authorize(Policy = AccessPolicies.Read)]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken) =>
        Ok(await _settings.GetConfigAsync(cancellationToken));

    [HttpPatch]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, string> changes, CancellationToken cancellationToken) =>
        (await _settings.SaveAsync(changes, cancellationToken)).ToActionResult("The settings have been saved");

    [HttpPatch("{key}")]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateConfigKeyRequest request, CancellationToken cancellationToken) =>
        (await _settings.SaveAsync(new Dictionary<string, string> { [key] = request.Value?.ToString() ?? "" }, cancellationToken))
            .ToActionResult($"The key '{key}' has been successfully updated");
}

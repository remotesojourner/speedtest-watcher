using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Web.Api.Contracts;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.Web.Api;

[ApiController]
[Route("api/config")]
[Tags("Settings")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly SettingsService _settings;

    public ConfigController(SettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Get the settings
    /// </summary>
    /// <remarks>
    /// Returns every setting the caller may see as a string, plus flags about sign-in. Read-only visitors don't get the settings marked full access only. Sign-in settings are managed on the Security tab and secrets are never returned.
    /// </remarks>
    /// <response code="200">The settings.</response>
    [HttpGet]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<ConfigResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigResponse>> GetConfig(CancellationToken cancellationToken) =>
        ConfigResponse.From(await _settings.GetConfigAsync(cancellationToken));

    /// <summary>
    /// Change settings
    /// </summary>
    /// <remarks>
    /// Saves one or more settings together: either every change is saved or none is. Keys are the setting names from <c>GET /api/config</c>, and values are strings. Send <c>none</c> to unset an optional value. Sign-in settings can only be changed on the Security tab.
    ///
    /// Open browser tabs, the scheduler and integrations pick the changes up straight away.
    /// </remarks>
    /// <param name="changes">The settings to change, as <c>{ "key": "value" }</c>.</param>
    /// <response code="200">Every change was saved.</response>
    /// <response code="400">A key is unknown or managed on the Security tab, or a value isn't allowed. Nothing was saved.</response>
    [HttpPatch]
    [Consumes("application/json")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> UpdateSettings([FromBody] Dictionary<string, string> changes, CancellationToken cancellationToken) =>
        (await _settings.SaveAsync(changes, cancellationToken)).ToActionResult("The settings have been saved");
}

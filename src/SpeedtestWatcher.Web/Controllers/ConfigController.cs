using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Settings;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ISettingsStore _store;
    private readonly SettingsService _settings;
    private readonly AuthSettings _auth;

    public ConfigController(ISettingsStore store, SettingsService settings, AuthSettings auth)
    {
        _store = store;
        _settings = settings;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        var auth = _auth.Current;

        var values = await _store.GetValuesAsync();
        var result = SettingDefinitions.All
            .Where(definition => definition.IsVisible(fullAccess: !isViewMode))
            .ToDictionary(definition => definition.Key, definition => (object?)values[definition.Key]);

        result["viewMode"] = isViewMode;
        result["authActive"] = auth.IsActive;
        result["authDisabledByEnv"] = auth.DisabledByEnvironment;
        if (!isViewMode)
        {
            result["oidcClientSecretSet"] = auth.ClientSecret != null;
            result["apiTokenSet"] = auth.ApiTokenHash != null;
        }

        return Ok(result);
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, string> changes)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        var result = await _settings.SaveAsync(changes);
        return result.Succeeded
            ? Ok(new { message = "The settings have been saved" })
            : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{key}")]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateConfigKeyRequest request)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        var result = await _settings.SaveAsync(new Dictionary<string, string> { [key] = request.Value?.ToString() ?? "" });
        return result.Succeeded
            ? Ok(new { message = $"The key '{key}' has been successfully updated" })
            : BadRequest(new { message = result.Error });
    }
}

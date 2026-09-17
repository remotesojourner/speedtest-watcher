using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Hubs;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ooklaId", "libreId", "libreUrl", "cron", "scheduleOffset", "ooklaServerIds", "libreServerIds", "internetCheckUrl", "skipIps"
    };

    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "oidcClientSecret", "apiTokenHash"
    };

    private readonly IConfigRepository _configRepo;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly IHubContext<SpeedtestHub> _hubContext;
    private readonly AuthSettings _auth;

    public ConfigController(
        IConfigRepository configRepo,
        IIntegrationDispatcher dispatcher,
        IHubContext<SpeedtestHub> hubContext,
        AuthSettings auth)
    {
        _configRepo = configRepo;
        _dispatcher = dispatcher;
        _hubContext = hubContext;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        var auth = _auth.Current;

        var allEntries = await _configRepo.ListAllAsync();
        var result = new Dictionary<string, object?>();

        foreach (var entry in allEntries)
        {
            if (SecretKeys.Contains(entry.Key)) continue;
            if (isViewMode && (SensitiveKeys.Contains(entry.Key) || AuthSettings.Keys.Contains(entry.Key))) continue;

            result[entry.Key] = entry.Value;
        }

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

    [HttpPatch("{key}")]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateConfigKeyRequest request)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        if (AuthSettings.Keys.Contains(key))
            return BadRequest(new { message = "Sign-in settings are changed on the Security tab" });

        var validationError = await _configRepo.ValidateInputAsync(key, request.Value);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        var stringValue = request.Value!.ToString()!;

        await _configRepo.UpdateValueAsync(key, stringValue);

        await _dispatcher.PublishAsync(new ConfigUpdated(key, stringValue));
        await _hubContext.Clients.All.SendAsync("ConfigChanged", key, stringValue);

        return Ok(new { message = $"The key '{key}' has been successfully updated" });
    }
}

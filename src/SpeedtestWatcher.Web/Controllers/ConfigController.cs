using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Hubs;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    // Hidden from read-only visitors: how tests are scheduled and routed, and the public IPs being watched for.
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ooklaId", "libreId", "libreUrl", "cron", "scheduleOffset", "ooklaServerIds", "libreServerIds", "internetCheckUrl", "skipIps"
    };

    // Stored secrets are never sent to anyone.
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
        bool isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        bool previewMode = Environment.GetEnvironmentVariable("PREVIEW_MODE") == "true";
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
        result["previewMode"] = previewMode;
        result["authActive"] = auth.IsActive;
        result["authDisabledByEnv"] = auth.DisabledByEnvironment;
        if (!isViewMode)
        {
            result["oidcClientSecretSet"] = auth.ClientSecret != null;
            result["apiTokenSet"] = auth.ApiTokenHash != null;
        }

        if (previewMode)
        {
            result["previewMessage"] = Environment.GetEnvironmentVariable("PREVIEW_MESSAGE")
                ?? "The owner of this instance has not provided a message";
        }

        if (result.Count == 0)
            return NotFound(new { message = "Hmm. There are no config values. Weird..." });

        return Ok(result);
    }

    [HttpPatch("{key}")]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateConfigKeyRequest request)
    {
        bool isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode)
            return Unauthorized(new { message = "Authentication required" });

        bool previewMode = Environment.GetEnvironmentVariable("PREVIEW_MODE") == "true";
        if (previewMode)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Cannot update configuration in preview mode" });

        // Sign-in settings are checked together before saving, so one can't be changed on its own here.
        if (AuthSettings.Keys.Contains(key))
            return BadRequest(new { message = "Sign-in settings are changed on the Security tab" });

        string? validationError = await _configRepo.ValidateInputAsync(key, request?.Value);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        string stringValue = request!.Value!.ToString()!;

        bool success = await _configRepo.UpdateValueAsync(key, stringValue);
        if (!success)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error updating the key '{key}'" });

        await _dispatcher.TriggerEventAsync(IntegrationEvent.ConfigUpdated, new { key, value = stringValue });
        await _hubContext.Clients.All.SendAsync("ConfigChanged", key, stringValue);

        return Ok(new { message = $"The key '{key}' has been successfully updated" });
    }
}

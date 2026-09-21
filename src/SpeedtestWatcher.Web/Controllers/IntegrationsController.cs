using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationRepository _repository;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly ISpeedtestRepository _speedtests;

    public IntegrationsController(IIntegrationRepository repository, IIntegrationDispatcher dispatcher, ISpeedtestRepository speedtests)
    {
        _repository = repository;
        _dispatcher = dispatcher;
        _speedtests = speedtests;
    }

    [HttpGet]
    public IActionResult GetSchemas()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        return Ok(_dispatcher.Schemas);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var list = await _repository.ListAllAsync();
        var dtos = list.Select(i =>
        {
            Dictionary<string, object?> dataDict;
            try
            {
                dataDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(i.Data) ?? new();
            }
            catch (JsonException)
            {
                dataDict = new();
            }

            return new ActiveIntegrationDto
            {
                Id = i.Id,
                Name = i.Name,
                DisplayName = i.DisplayName,
                Data = dataDict,
                LastActivity = i.LastActivity,
                ActivityFailed = i.ActivityFailed
            };
        });

        return Ok(dtos);
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> Create(string name, [FromBody] Dictionary<string, object?> body)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var schemas = _dispatcher.Schemas;
        if (!schemas.ContainsKey(name))
            return NotFound(new { message = "Integration not found" });

        var displayName = "Untitled";
        if (body.TryGetValue("integration_name", out var dn) && dn != null)
        {
            displayName = dn.ToString()!;
            body.Remove("integration_name");
        }

        var json = JsonSerializer.Serialize(body);
        var id = await _repository.CreateAsync(name, displayName, json);
        return Ok(new { message = "Integration created", id });
    }

    [HttpPost("{name}/test")]
    public async Task<IActionResult> SendTest(string name, [FromBody] Dictionary<string, object?> body, [FromQuery] string? id, CancellationToken cancellationToken)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        if (!_dispatcher.Schemas.ContainsKey(name))
            return NotFound(new { message = "Integration not found" });

        var sample = await _speedtests.GetLatestCompletedAsync(cancellationToken) ?? SampleResult();
        var result = await _dispatcher.TestAsync(name, id ?? "unsaved", JsonSerializer.Serialize(body), sample, cancellationToken);

        return Ok(new IntegrationTestResultDto
        {
            Success = result.Outcome != IntegrationOutcome.Failed,
            Message = result.Error
        });
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object?> body)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Integration not found" });

        string? displayName = null;
        if (body.TryGetValue("integration_name", out var dn) && dn != null)
        {
            displayName = dn.ToString();
            body.Remove("integration_name");
        }

        Dictionary<string, object?> currentData;
        try
        {
            currentData = JsonSerializer.Deserialize<Dictionary<string, object?>>(existing.Data) ?? new();
        }
        catch (JsonException)
        {
            currentData = new();
        }

        foreach (var (k, v) in body)
            currentData[k] = v;

        await _repository.PatchAsync(id, displayName, JsonSerializer.Serialize(currentData));
        return Ok(new { message = "Integration updated" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var deleted = await _repository.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Integration not found" });

        return Ok(new { message = "Integration deleted" });
    }

    private static Speedtest SampleResult() => new()
    {
        ServerName = "Sample server",
        Ping = 15,
        Jitter = 1.2,
        Download = 250,
        Upload = 50,
        Status = TestStatus.Completed,
        Healthy = true,
        Type = TestType.Custom
    };
}

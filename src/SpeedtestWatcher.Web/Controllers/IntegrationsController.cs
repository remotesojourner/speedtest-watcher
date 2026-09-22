using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private const string DisplayNameKey = "integration_name";

    private readonly IntegrationService _integrations;

    public IntegrationsController(IntegrationService integrations)
    {
        _integrations = integrations;
    }

    [HttpGet]
    public IActionResult GetSchemas() => Ok(_integrations.Schemas);

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken) => Ok(await _integrations.ListAsync(cancellationToken));

    [HttpPut("{name}")]
    public async Task<IActionResult> Create(string name, [FromBody] Dictionary<string, JsonElement> body, CancellationToken cancellationToken)
    {
        var displayName = TakeDisplayName(body);
        return (await _integrations.CreateAsync(name, displayName, body, cancellationToken))
            .ToActionResult(id => Ok(new { message = "Integration created", id }));
    }

    [HttpPost("{name}/test")]
    public async Task<IActionResult> SendTest(string name, [FromBody] Dictionary<string, JsonElement> body, [FromQuery] string? id, CancellationToken cancellationToken) =>
        (await _integrations.SendTestAsync(name, body, id, cancellationToken)).ToActionResult();

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, JsonElement> body, CancellationToken cancellationToken)
    {
        var displayName = TakeDisplayName(body);
        return (await _integrations.UpdateAsync(id, displayName, body, cancellationToken)).ToActionResult("Integration updated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        (await _integrations.DeleteAsync(id, cancellationToken)).ToActionResult("Integration deleted");

    private static string? TakeDisplayName(Dictionary<string, JsonElement> body) =>
        body.Remove(DisplayNameKey, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

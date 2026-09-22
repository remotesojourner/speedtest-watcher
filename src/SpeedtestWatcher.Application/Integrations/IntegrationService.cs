using System.Text.Json;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed class IntegrationService
{
    private const string NotFoundMessage = "Integration not found";

    private readonly IIntegrationRepository _integrations;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly ISpeedtestRepository _results;
    private readonly ICurrentAccess _access;

    public IntegrationService(IIntegrationRepository integrations, IIntegrationDispatcher dispatcher, ISpeedtestRepository results, ICurrentAccess access)
    {
        _integrations = integrations;
        _dispatcher = dispatcher;
        _results = results;
        _access = access;
    }

    public IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas => _dispatcher.Schemas;

    public async Task<IReadOnlyList<ActiveIntegrationDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _integrations.ListAllAsync(cancellationToken)).Select(integration => new ActiveIntegrationDto
        {
            Id = integration.Id,
            Name = integration.Name,
            DisplayName = integration.DisplayName,
            Data = ReadValues(integration.Data),
            LastActivity = integration.LastActivity,
            ActivityFailed = integration.ActivityFailed
        }).ToList();

    public async Task<OperationResult<string>> CreateAsync(
        string type, string? displayName, IReadOnlyDictionary<string, JsonElement> settings, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!_dispatcher.Schemas.TryGetValue(type, out var schema)) return OperationResult.NotFound(NotFoundMessage);
        if (IntegrationSettingsRules.ProblemWith(schema, settings, settings.Keys) is { } problem) return OperationResult.Invalid(problem);

        var id = await _integrations.CreateAsync(type, displayName ?? "", JsonSerializer.Serialize(settings), cancellationToken);
        return OperationResult.Ok(id);
    }

    public async Task<OperationResult> UpdateAsync(
        string id, string? displayName, IReadOnlyDictionary<string, JsonElement> settings, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (await _integrations.GetByIdAsync(id, cancellationToken) is not { } existing) return OperationResult.NotFound(NotFoundMessage);
        if (!_dispatcher.Schemas.TryGetValue(existing.Name, out var schema)) return OperationResult.Invalid($"{existing.Name} isn't a known integration type");

        var merged = ReadSettings(existing.Data);
        foreach (var (key, value) in settings) merged[key] = value;

        if (IntegrationSettingsRules.ProblemWith(schema, merged, settings.Keys) is { } problem) return OperationResult.Invalid(problem);

        await _integrations.PatchAsync(id, displayName, JsonSerializer.Serialize(merged), cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return await _integrations.DeleteAsync(id, cancellationToken) ? OperationResult.Ok() : OperationResult.NotFound(NotFoundMessage);
    }

    public async Task<OperationResult<IntegrationTestResultDto>> SendTestAsync(
        string type, IReadOnlyDictionary<string, JsonElement> settings, string? id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!_dispatcher.Schemas.TryGetValue(type, out var schema)) return OperationResult.NotFound(NotFoundMessage);
        if (IntegrationSettingsRules.ProblemWith(schema, settings, settings.Keys) is { } problem) return OperationResult.Invalid(problem);

        var sample = await _results.GetLatestCompletedAsync(cancellationToken) ?? SampleResult();
        var result = await _dispatcher.TestAsync(type, id ?? "unsaved", JsonSerializer.Serialize(settings), sample, cancellationToken);

        return OperationResult.Ok(new IntegrationTestResultDto
        {
            Success = result.Outcome != IntegrationOutcome.Failed,
            Message = result.Error
        });
    }

    private static Dictionary<string, JsonElement> ReadSettings(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static Dictionary<string, object?> ReadValues(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
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

using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

internal class IntegrationDispatcher : IIntegrationDispatcher
{
    private readonly IIntegrationRepository _repository;
    private readonly IReadOnlyDictionary<string, IIntegration> _integrations;
    private readonly HeartbeatSchedule _heartbeats;
    private readonly ILogger<IntegrationDispatcher> _logger;

    public IntegrationDispatcher(
        IIntegrationRepository repository,
        IEnumerable<IIntegration> integrations,
        HeartbeatSchedule heartbeats,
        ILogger<IntegrationDispatcher> logger)
    {
        _repository = repository;
        _heartbeats = heartbeats;
        _logger = logger;

        var byName = new Dictionary<string, IIntegration>(StringComparer.OrdinalIgnoreCase);
        foreach (var integration in integrations) byName[integration.Name] = integration;
        _integrations = byName;
        Schemas = byName.ToDictionary(pair => pair.Key, pair => pair.Value.Schema, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; }

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var stored = await _repository.ListAllAsync(cancellationToken);

        foreach (var integration in stored)
        {
            var settings = IntegrationSettings.Parse(integration.Data);
            if (settings == null)
            {
                _logger.LogWarning("Integration {Name} ({Id}) was not run because its saved settings can't be read", integration.Name, integration.Id);
                await _repository.UpdateActivityAsync(integration.Id, true, cancellationToken);
                continue;
            }

            if (integrationEvent is Heartbeat && !_heartbeats.IsDue(integration.Id, settings.GetInt("interval", 1))) continue;

            try
            {
                var result = await HandleAsync(integration, integrationEvent, settings, cancellationToken);
                if (result.Outcome == IntegrationOutcome.NotApplicable) continue;

                if (result.Outcome == IntegrationOutcome.Failed)
                    _logger.LogWarning("Integration {Name} ({Id}) failed: {Error}", integration.Name, integration.Id, result.Error);

                await _repository.UpdateActivityAsync(integration.Id, result.Outcome == IntegrationOutcome.Failed, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Failed to dispatch integration {Name} ({Id})", integration.Name, integration.Id);
                await _repository.UpdateActivityAsync(integration.Id, true, cancellationToken);
            }
        }
    }

    public async Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default)
    {
        if (!_integrations.TryGetValue(name, out var integration)) return UnknownType(name);
        if (IntegrationSettings.Parse(settingsJson) is not { } settings) return IntegrationResult.Failed("The settings can't be read");

        try
        {
            return await integration.SendTestAsync(new IntegrationContext(id, settings), sample, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Sending a test for integration {Name} ({Id}) failed", name, id);
            return IntegrationResult.Failed(ex.Message);
        }
    }

    private static IntegrationResult UnknownType(string name) => IntegrationResult.Failed($"{name} isn't a known integration type");

    private Task<IntegrationResult> HandleAsync(IntegrationData integration, IntegrationEvent integrationEvent, IntegrationSettings settings, CancellationToken cancellationToken) =>
        _integrations.TryGetValue(integration.Name, out var handler)
            ? handler.HandleAsync(integrationEvent, new IntegrationContext(integration.Id, settings), cancellationToken)
            : Task.FromResult(UnknownType(integration.Name));
}

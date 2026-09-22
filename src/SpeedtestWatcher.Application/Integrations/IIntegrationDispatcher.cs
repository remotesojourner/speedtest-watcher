using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public interface IIntegrationDispatcher
{
    IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; }
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
    Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default);
}

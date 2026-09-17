using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Interfaces;

public interface IIntegrationDispatcher
{
    IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; }
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
    Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default);
}

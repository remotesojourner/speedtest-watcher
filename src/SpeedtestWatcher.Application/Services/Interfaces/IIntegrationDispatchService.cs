using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Models.Entities;
using SpeedtestWatcher.Application.Models.Events;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IIntegrationDispatchService
{
    IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; }
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
    Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default);
}

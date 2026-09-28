using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Models.Entities;
using SpeedtestWatcher.Application.Models.Events;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal sealed class RecordingDispatcher : IIntegrationDispatchService
{
    private readonly object _gate = new();
    private readonly List<IntegrationEvent> _events = [];

    public List<IntegrationEvent> Events
    {
        get
        {
            lock (_gate) return _events.ToList();
        }
    }

    public IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; } = new Dictionary<string, IntegrationTypeSchemaDto>();

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        lock (_gate) _events.Add(integrationEvent);
        return Task.CompletedTask;
    }

    public Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

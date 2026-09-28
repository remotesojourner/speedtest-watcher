using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Models.Entities;
using SpeedtestWatcher.Application.Models.Events;

namespace SpeedtestWatcher.Application.Services.Integrations.Interfaces;

public interface IIntegrationTypeService
{
    string Name { get; }

    IntegrationTypeSchemaDto Schema { get; }

    Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken);

    Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken);
}

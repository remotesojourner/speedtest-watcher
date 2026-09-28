using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Models.Events;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.BackgroundServices;

internal class IntegrationTickerService : PeriodicBackgroundService
{
    public IntegrationTickerService(IServiceScopeFactory scopeFactory, ILogger<IntegrationTickerService> logger) : base(scopeFactory, logger)
    {
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken) =>
        services.GetRequiredService<IIntegrationDispatchService>().PublishAsync(new Heartbeat(), stoppingToken);
}

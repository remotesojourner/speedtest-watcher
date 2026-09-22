using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Integrations;

internal class IntegrationTickerService : PeriodicBackgroundService
{
    public IntegrationTickerService(IServiceScopeFactory scopeFactory, ILogger<IntegrationTickerService> logger) : base(scopeFactory, logger)
    {
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken) =>
        services.GetRequiredService<IIntegrationDispatcher>().PublishAsync(new Heartbeat(), stoppingToken);
}

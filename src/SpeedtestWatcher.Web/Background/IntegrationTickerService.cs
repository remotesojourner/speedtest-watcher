using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Background;

public class IntegrationTickerService : PeriodicBackgroundService
{
    public IntegrationTickerService(IServiceScopeFactory scopeFactory, ILogger<IntegrationTickerService> logger) : base(scopeFactory, logger)
    {
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken) =>
        services.GetRequiredService<IIntegrationDispatcher>().PublishAsync(new Heartbeat(), stoppingToken);
}

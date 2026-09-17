using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Background;

public class InterfaceRefreshService : PeriodicBackgroundService
{
    public InterfaceRefreshService(IServiceScopeFactory scopeFactory, ILogger<InterfaceRefreshService> logger) : base(scopeFactory, logger)
    {
    }

    protected override TimeSpan Interval => TimeSpan.FromHours(1);

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken) =>
        services.GetRequiredService<INetworkInterfaceDetector>().GetInterfacesAsync(forceRefresh: true, stoppingToken);
}

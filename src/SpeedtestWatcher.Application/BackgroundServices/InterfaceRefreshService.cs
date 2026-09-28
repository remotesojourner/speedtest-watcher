using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.BackgroundServices;

internal class InterfaceRefreshService : PeriodicBackgroundService
{
    public InterfaceRefreshService(IServiceScopeFactory scopeFactory, ILogger<InterfaceRefreshService> logger) : base(scopeFactory, logger)
    {
    }

    protected override TimeSpan Interval => TimeSpan.FromHours(1);

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken) =>
        services.GetRequiredService<INetworkInterfaceService>().GetInterfacesAsync(forceRefresh: true, stoppingToken);
}

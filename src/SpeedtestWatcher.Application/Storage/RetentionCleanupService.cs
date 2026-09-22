using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

internal class RetentionCleanupService : PeriodicBackgroundService
{
    private readonly ILogger<RetentionCleanupService> _logger;

    public RetentionCleanupService(IServiceScopeFactory scopeFactory, ILogger<RetentionCleanupService> logger) : base(scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        var retentionDays = (await services.GetRequiredService<ISettingsStore>().GetAsync(stoppingToken)).RetentionDays;
        if (retentionDays <= 0) return;

        var deleted = await services.GetRequiredService<ISpeedtestRepository>().RemoveOldTestsAsync(retentionDays, stoppingToken);
        if (deleted > 0)
        {
            _logger.LogInformation("Pruned {Count} speedtests older than {Days} days", deleted, retentionDays);
        }
    }
}

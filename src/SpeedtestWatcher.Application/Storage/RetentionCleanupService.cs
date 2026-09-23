using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

internal partial class RetentionCleanupService : PeriodicBackgroundService
{
    private const int DaysOfProbeRounds = 30;

    private readonly ILogger<RetentionCleanupService> _logger;

    public RetentionCleanupService(IServiceScopeFactory scopeFactory, ILogger<RetentionCleanupService> logger) : base(scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        var monitoring = services.GetRequiredService<IMonitoringRepository>();
        await monitoring.RemoveOldRoundsAsync(DaysOfProbeRounds, stoppingToken);

        var retentionDays = (await services.GetRequiredService<ISettingsStore>().GetAsync(stoppingToken)).RetentionDays;
        if (retentionDays <= 0) return;

        await monitoring.RemoveOldOutagesAsync(retentionDays, stoppingToken);

        var deleted = await services.GetRequiredService<ISpeedtestRepository>().RemoveOldTestsAsync(retentionDays, stoppingToken);
        if (deleted > 0)
        {
            LogPruned(deleted, retentionDays);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pruned {Count} speedtests older than {Days} days")]
    private partial void LogPruned(int count, int days);
}

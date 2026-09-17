using System.Globalization;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Background;

public class RetentionCleanupService : PeriodicBackgroundService
{
    private readonly ILogger<RetentionCleanupService> _logger;

    public RetentionCleanupService(IServiceScopeFactory scopeFactory, ILogger<RetentionCleanupService> logger) : base(scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        var days = await services.GetRequiredService<IConfigRepository>().GetValueAsync("retentionDays", stoppingToken);
        if (!int.TryParse(days, NumberStyles.Integer, CultureInfo.InvariantCulture, out var retentionDays) || retentionDays <= 0) return;

        var deleted = await services.GetRequiredService<ISpeedtestRepository>().RemoveOldTestsAsync(retentionDays, stoppingToken);
        if (deleted > 0)
        {
            _logger.LogInformation("Pruned {Count} speedtests older than {Days} days", deleted, retentionDays);
        }
    }
}

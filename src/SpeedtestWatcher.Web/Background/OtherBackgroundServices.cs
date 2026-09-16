using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Background;

public class RetentionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionCleanupService> _logger;

    public RetentionCleanupService(IServiceProvider serviceProvider, ILogger<RetentionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var configRepo = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
                var speedtestRepo = scope.ServiceProvider.GetRequiredService<ISpeedtestRepository>();

                string? daysStr = await configRepo.GetValueAsync("retentionDays", stoppingToken);
                if (int.TryParse(daysStr, out int days) && days > 0)
                {
                    int deleted = await speedtestRepo.RemoveOldTestsAsync(days, stoppingToken);
                    if (deleted > 0)
                    {
                        _logger.LogInformation("Pruned {Count} speedtests older than {Days} days", deleted, days);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during retention pruning");
            }

            if (!await BackgroundDelay.WaitAsync(TimeSpan.FromSeconds(60), stoppingToken))
            {
                break;
            }
        }
    }
}

public class IntegrationTickerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationTickerService> _logger;

    public IntegrationTickerService(IServiceProvider serviceProvider, ILogger<IntegrationTickerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationDispatcher>();
                await dispatcher.TriggerEventAsync(IntegrationEvent.MinutePassed, null, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in integration minute ticker");
            }

            if (!await BackgroundDelay.WaitAsync(TimeSpan.FromSeconds(60), stoppingToken))
            {
                break;
            }
        }
    }
}

public class InterfaceRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InterfaceRefreshService> _logger;

    public InterfaceRefreshService(IServiceProvider serviceProvider, ILogger<InterfaceRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var detector = scope.ServiceProvider.GetRequiredService<INetworkInterfaceDetector>();
                await detector.GetInterfacesAsync(true, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in interface refresh service");
            }

            if (!await BackgroundDelay.WaitAsync(TimeSpan.FromHours(1), stoppingToken))
            {
                break;
            }
        }
    }
}

public class CliDownloadService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CliDownloadService> _logger;

    public CliDownloadService(IServiceProvider serviceProvider, ILogger<CliDownloadService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Environment.GetEnvironmentVariable("PREVIEW_MODE") == "true")
        {
            _logger.LogInformation("Skipping CLI binary download in preview mode");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cliManager = scope.ServiceProvider.GetRequiredService<ICliManager>();
            await cliManager.EnsureBinariesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download CLI binaries during startup");
        }
    }
}

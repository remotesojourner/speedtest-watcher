using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Background;

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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpeedtestWatcher.Application.Providers;

internal partial class CliDownloadService : BackgroundService
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
            LogDownloadFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to download CLI binaries during startup")]
    private partial void LogDownloadFailed(Exception exception);
}

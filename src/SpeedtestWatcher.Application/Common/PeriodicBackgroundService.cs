using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpeedtestWatcher.Application.Common;

internal abstract partial class PeriodicBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected PeriodicBackgroundService(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected abstract TimeSpan Interval { get; }

    protected abstract Task RunOnceAsync(IServiceProvider services, CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunInScopeAsync(stoppingToken);
        }
        while (await WaitForNextRunAsync(timer, stoppingToken));
    }

    private async Task RunInScopeAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await RunOnceAsync(scope.ServiceProvider, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            LogRunFailed(ex, GetType().Name, Interval);
        }
    }

    private static async Task<bool> WaitForNextRunAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Service} failed and runs again in {Interval}")]
    private partial void LogRunFailed(Exception exception, string service, TimeSpan interval);
}

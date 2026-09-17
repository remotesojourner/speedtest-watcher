namespace SpeedtestWatcher.Web.Background;

public abstract class PeriodicBackgroundService : BackgroundService
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
            _logger.LogWarning(ex, "{Service} failed and runs again in {Interval}", GetType().Name, Interval);
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
}

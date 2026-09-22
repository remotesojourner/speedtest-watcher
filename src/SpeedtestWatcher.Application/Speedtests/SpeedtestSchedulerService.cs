using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Application.Speedtests;

internal sealed partial class SpeedtestSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IAppEvents _events;
    private readonly TimeProvider _time;
    private readonly bool _runTestOnStartup;
    private readonly ILogger<SpeedtestSchedulerService> _logger;
    private CancellationTokenSource _scheduleChanged = new();

    public SpeedtestSchedulerService(
        IServiceScopeFactory scopes, IAppEvents events, TimeProvider time, IOptions<SpeedtestWatcherOptions> options, ILogger<SpeedtestSchedulerService> logger)
    {
        _scopes = scopes;
        _events = events;
        _time = time;
        _runTestOnStartup = options.Value.RunTestOnStartup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _events.SettingsChanged += OnSettingsChanged;
        try
        {
            if (_runTestOnStartup && await BackgroundDelay.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken)) await RunAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await WaitForNextRunAsync(stoppingToken)) await RunAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogLoopFailed(ex);
                    await BackgroundDelay.WaitAsync(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
        finally
        {
            _events.SettingsChanged -= OnSettingsChanged;
        }
    }

    public override void Dispose()
    {
        _scheduleChanged.Dispose();
        base.Dispose();
    }

    private async Task<bool> WaitForNextRunAsync(CancellationToken stoppingToken)
    {
        var scheduleChanged = _scheduleChanged.Token;
        var due = (await ReadScheduleAsync(stoppingToken)).NextRunAfter(_time.GetUtcNow().UtcDateTime) ?? DateTime.MaxValue;
        if (!await BackgroundDelay.WaitUntilAsync(due, _time, stoppingToken, scheduleChanged)) return false;

        if ((await ReadScheduleAsync(stoppingToken)).RandomOffset)
        {
            var offset = TimeSpan.FromSeconds(Random.Shared.Next(30, 300));
            LogOffset(offset.TotalSeconds);
            await Task.Delay(offset, _time, stoppingToken);
        }

        return true;
    }

    private void OnSettingsChanged(IReadOnlyDictionary<string, string> changes)
    {
        if (changes.ContainsKey("cron")) Interlocked.Exchange(ref _scheduleChanged, new CancellationTokenSource()).Cancel();
    }

    private async Task<ScheduleSettings> ReadScheduleAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopes.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<ISettingsStore>().GetAsync(stoppingToken)).Schedule;
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SpeedtestRunService>().RunAsync(TestType.Auto, cancellationToken: stoppingToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in speedtest scheduler loop")]
    private partial void LogLoopFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying random schedule offset of {Seconds}s")]
    private partial void LogOffset(double seconds);
}

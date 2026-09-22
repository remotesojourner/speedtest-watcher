using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Web.Background;

public sealed class SpeedtestSchedulerService : BackgroundService
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
                    _logger.LogError(ex, "Error in speedtest scheduler loop");
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
            _logger.LogInformation("Applying random schedule offset of {Seconds}s", offset.TotalSeconds);
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
}

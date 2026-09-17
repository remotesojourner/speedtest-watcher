using System.Globalization;
using Cronos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.SpeedTest;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Background;

public class SpeedtestSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPauseStateService _pauseState;
    private readonly IHubContext<SpeedtestHub> _hubContext;
    private readonly ILogger<SpeedtestSchedulerService> _logger;
    private readonly bool _runTestOnStartup;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public SpeedtestSchedulerService(
        IServiceProvider serviceProvider,
        IPauseStateService pauseState,
        IHubContext<SpeedtestHub> hubContext,
        IOptions<SpeedtestWatcherOptions> options,
        ILogger<SpeedtestSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _pauseState = pauseState;
        _hubContext = hubContext;
        _runTestOnStartup = options.Value.RunTestOnStartup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Speedtest scheduler service started");

        if (_runTestOnStartup)
        {
            _ = Task.Run(async () =>
            {
                if (await BackgroundDelay.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken))
                {
                    await ExecuteSpeedtestAsync("auto", cancellationToken: stoppingToken);
                }
            }, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var configRepo = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
                var savedCron = await configRepo.GetValueAsync("cron", stoppingToken) ?? "0 * * * *";

                CronExpression cron;
                try
                {
                    cron = CronExpression.Parse(savedCron, CronFormat.Standard);
                }
                catch (CronFormatException ex)
                {
                    _logger.LogWarning(ex, "The saved schedule {Cron} is not a valid cron expression, so tests run hourly", savedCron);
                    cron = CronExpression.Parse("0 * * * *", CronFormat.Standard);
                }

                var nextUtc = cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
                if (!nextUtc.HasValue)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                var delay = nextUtc.Value - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                var scheduleOffset = await configRepo.GetValueAsync("scheduleOffset", stoppingToken) ?? "true";
                if (scheduleOffset == "true")
                {
                    var randomOffsetSeconds = Random.Shared.Next(30, 300);
                    _logger.LogInformation("Applying random schedule offset of {Seconds}s", randomOffsetSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(randomOffsetSeconds), stoppingToken);
                }

                if (_pauseState.IsPaused)
                {
                    _logger.LogInformation("Speedtest skipped because tests are paused");
                    continue;
                }

                await ExecuteSpeedtestAsync("auto", cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in speedtest scheduler loop");
                if (!await BackgroundDelay.WaitAsync(TimeSpan.FromMinutes(1), stoppingToken))
                {
                    break;
                }
            }
        }
    }

    public async Task<SpeedtestExecutionResult> ExecuteSpeedtestAsync(string type = "auto", string? serverOverride = null, CancellationToken cancellationToken = default)
    {
        if (_pauseState.IsRunning)
        {
            return new SpeedtestExecutionResult { Success = false, Error = "Speedtest is already running" };
        }

        if (_pauseState.IsPaused && type == "auto")
        {
            return new SpeedtestExecutionResult { Success = false, Error = "Speedtest is paused" };
        }

        if (!await _runLock.WaitAsync(100, cancellationToken))
        {
            return new SpeedtestExecutionResult { Success = false, Error = "Speedtest is already running" };
        }

        try
        {
            _pauseState.SetRunning(true);
            await _hubContext.Clients.All.SendAsync("TestStarted", cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            return await RunWithOneRetryAsync(scope.ServiceProvider, type, serverOverride, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error running speedtest");
            return new SpeedtestExecutionResult { Success = false, Error = ex.Message };
        }
        finally
        {
            _pauseState.SetRunning(false);
            _runLock.Release();
        }
    }

    private async Task<SpeedtestExecutionResult> RunWithOneRetryAsync(IServiceProvider services, string type, string? serverOverride, CancellationToken cancellationToken)
    {
        var configRepo = services.GetRequiredService<IConfigRepository>();
        var speedtestRepo = services.GetRequiredService<ISpeedtestRepository>();
        var runner = services.GetRequiredService<ISpeedtestRunner>();
        var dispatcher = services.GetRequiredService<IIntegrationDispatcher>();
        var recommendationRepo = services.GetRequiredService<IRecommendationRepository>();
        var serverSelector = services.GetRequiredService<ServerSelector>();
        var connectivity = services.GetRequiredService<ConnectivityChecker>();

        var providerStr = await configRepo.GetValueAsync("provider", cancellationToken) ?? "none";
        if (!Enum.TryParse<SpeedtestProvider>(providerStr, true, out var provider) || provider == SpeedtestProvider.None)
        {
            return new SpeedtestExecutionResult { Success = false, Error = "No provider selected" };
        }

        var libreUrl = provider == SpeedtestProvider.Libre ? await configRepo.GetValueAsync("libreUrl", cancellationToken) : null;
        var networkInterface = await configRepo.GetValueAsync("interface", cancellationToken);
        if (libreUrl == "none") libreUrl = null;

        var check = await connectivity.CheckAsync(cancellationToken);
        if (!check.Proceed)
        {
            var reason = check.SkipReason ?? "Skipped";
            await RecordSkippedAsync(speedtestRepo, dispatcher, type, reason, cancellationToken);
            return new SpeedtestExecutionResult { Success = false, Skipped = true, Error = reason };
        }

        var serverId = serverOverride ?? await serverSelector.SelectAsync(provider, cancellationToken);

        await dispatcher.PublishAsync(new TestStarted(providerStr, type), cancellationToken);

        var result = await runner.RunTestAsync(provider, serverId, libreUrl, networkInterface, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("Speedtest failed ({Error}). Retrying once...", result.Error);
            serverId = serverOverride ?? await serverSelector.SelectAsync(provider, cancellationToken);
            result = await runner.RunTestAsync(provider, serverId, libreUrl, networkInterface, cancellationToken);
        }

        var thresholds = await ThresholdsAsync(configRepo, cancellationToken);
        var testEntity = new Speedtest
        {
            ServerId = result.ServerId,
            ServerName = result.ServerName,
            ServerHost = result.ServerHost,
            Ping = result.Success ? result.Ping : -1,
            Jitter = result.Success ? result.Jitter : null,
            Download = result.Success ? result.Download : -1,
            Upload = result.Success ? result.Upload : -1,
            Time = result.Time,
            Type = type,
            ResultId = result.ResultId,
            Error = result.Success ? null : result.Error ?? "Unknown error",
            Status = result.Success ? "completed" : "failed",
            Healthy = result.Success ? thresholds.Evaluate(result.Ping, result.Download, result.Upload) : null,
            ThresholdPing = thresholds.Ping,
            ThresholdDownload = thresholds.Download,
            ThresholdUpload = thresholds.Upload,
            Created = DateTime.UtcNow
        };

        var testId = await speedtestRepo.CreateAsync(testEntity, cancellationToken);
        testEntity.Id = testId;

        var dto = SpeedtestDto.From(testEntity);

        if (result.Success)
        {
            var newRecommendation = await recommendationRepo.RecalculateAsync(cancellationToken);
            await dispatcher.PublishAsync(new TestFinished(testEntity), cancellationToken);
            if (testEntity.Healthy == false)
                await dispatcher.PublishAsync(new TestUnhealthy(testEntity), cancellationToken);
            if (newRecommendation != null)
                await dispatcher.PublishAsync(new RecommendationsUpdated(newRecommendation), cancellationToken);
            await _hubContext.Clients.All.SendAsync("NewTestResult", dto, cancellationToken);
        }
        else
        {
            await dispatcher.PublishAsync(new TestFailed(testEntity), cancellationToken);
            await _hubContext.Clients.All.SendAsync("TestFailed", dto, cancellationToken);
        }

        return result;
    }

    private async Task RecordSkippedAsync(
        ISpeedtestRepository speedtestRepo,
        IIntegrationDispatcher dispatcher,
        string type,
        string reason,
        CancellationToken cancellationToken)
    {
        var skipped = new Speedtest
        {
            Ping = -1,
            Download = -1,
            Upload = -1,
            Status = "skipped",
            Error = reason,
            Type = type,
            Created = DateTime.UtcNow
        };

        skipped.Id = await speedtestRepo.CreateAsync(skipped, cancellationToken);
        _logger.LogInformation("Speedtest skipped: {Reason}", reason);

        await dispatcher.PublishAsync(new TestSkipped(skipped), cancellationToken);
        await _hubContext.Clients.All.SendAsync("NewTestResult", SpeedtestDto.From(skipped), cancellationToken);
    }

    private sealed record Thresholds(int? Ping, double? Download, double? Upload)
    {
        public bool? Evaluate(int ping, double download, double upload)
        {
            if (Ping == null && Download == null && Upload == null) return null;
            if (Ping is { } maxPing && ping > maxPing) return false;
            if (Download is { } minDownload && download < minDownload) return false;
            if (Upload is { } minUpload && upload < minUpload) return false;
            return true;
        }
    }

    private static async Task<Thresholds> ThresholdsAsync(IConfigRepository configRepo, CancellationToken cancellationToken)
    {
        return new Thresholds(
            ParseInt(await configRepo.GetValueAsync("ping", cancellationToken)),
            ParseDouble(await configRepo.GetValueAsync("download", cancellationToken)),
            ParseDouble(await configRepo.GetValueAsync("upload", cancellationToken)));

        static int? ParseInt(string? raw) =>
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : null;

        static double? ParseDouble(string? raw) =>
            double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : null;
    }
}

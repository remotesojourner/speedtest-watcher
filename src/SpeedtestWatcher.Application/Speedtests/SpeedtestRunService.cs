using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Speedtests;

public sealed partial class SpeedtestRunService
{
    public const string AlreadyRunning = "Speedtest is already running";

    private readonly RunState _state;
    private readonly ISettingsStore _settings;
    private readonly ISpeedtestRepository _results;
    private readonly ISpeedtestRunner _runner;
    private readonly IConnectivityChecker _connectivity;
    private readonly BufferbloatMeter _bufferbloat;
    private readonly ServerSelector _serverSelector;
    private readonly RecommendationService _recommendations;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly IAppEvents _events;
    private readonly ICurrentAccess _access;
    private readonly IServiceScopeFactory _scopes;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SpeedtestRunService> _logger;

    public SpeedtestRunService(
        RunState state,
        ISettingsStore settings,
        ISpeedtestRepository results,
        ISpeedtestRunner runner,
        IConnectivityChecker connectivity,
        BufferbloatMeter bufferbloat,
        ServerSelector serverSelector,
        RecommendationService recommendations,
        IIntegrationDispatcher dispatcher,
        IAppEvents events,
        ICurrentAccess access,
        IServiceScopeFactory scopes,
        IHostApplicationLifetime lifetime,
        ILogger<SpeedtestRunService> logger)
    {
        _state = state;
        _settings = settings;
        _results = results;
        _runner = runner;
        _connectivity = connectivity;
        _bufferbloat = bufferbloat;
        _serverSelector = serverSelector;
        _recommendations = recommendations;
        _dispatcher = dispatcher;
        _events = events;
        _access = access;
        _scopes = scopes;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<SpeedtestExecutionResult> RunAsync(TestType type, string? serverOverride = null, CancellationToken cancellationToken = default)
    {
        if (type == TestType.Auto && _state.IsPaused)
        {
            LogSkippedWhilePaused();
            return Failure("Speedtest is paused");
        }

        if (!_state.TryStartRun()) return Failure(AlreadyRunning);

        return await RunClaimedAsync(type, serverOverride, cancellationToken);
    }

    public async Task<OperationResult> StartManualRunAsync(string? serverId, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (_state.IsRunning) return OperationResult.Conflict(AlreadyRunning);

        if ((await _settings.GetAsync(cancellationToken)).Provider.Selected == SpeedtestProvider.None)
            return OperationResult.Conflict("No speedtest provider selected");

        if (_state.IsPaused) return OperationResult.Conflict("Speedtests are paused");
        if (!_state.TryStartRun()) return OperationResult.Conflict(AlreadyRunning);

        _ = Task.Run(() => RunClaimedInOwnScopeAsync(serverId), CancellationToken.None);
        return OperationResult.Ok();
    }

    private async Task RunClaimedInOwnScopeAsync(string? serverId)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<SpeedtestRunService>()
                .RunClaimedAsync(TestType.Custom, serverId, _lifetime.ApplicationStopping);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            LogManualRunFailed(ex);
            _state.FinishRun();
        }
    }

    private async Task<SpeedtestExecutionResult> RunClaimedAsync(TestType type, string? serverOverride, CancellationToken cancellationToken)
    {
        try
        {
            _events.PublishTestStarted();
            return await RunWithOneRetryAsync(type, serverOverride, cancellationToken);
        }
        catch (Exception ex)
        {
            LogRunCrashed(ex);
            return Failure(ex.Message);
        }
        finally
        {
            _state.FinishRun();
        }
    }

    private async Task<SpeedtestExecutionResult> RunWithOneRetryAsync(TestType type, string? serverOverride, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        var provider = settings.Provider.Selected;
        if (provider == SpeedtestProvider.None) return Failure("No provider selected");

        var check = await _connectivity.CheckAsync(settings.PreTestChecks, cancellationToken);
        if (!check.Proceed)
        {
            var reason = check.SkipReason ?? "Skipped";
            await RecordSkippedAsync(type, reason, check.PublicIp, cancellationToken);
            return new SpeedtestExecutionResult { Success = false, Skipped = true, Error = reason };
        }

        var libreUrl = provider == SpeedtestProvider.Libre ? settings.Provider.LibreUrl : null;
        var serverId = serverOverride ?? await _serverSelector.SelectAsync(settings.Provider, cancellationToken);

        await _dispatcher.PublishAsync(new TestStarted(provider, type), cancellationToken);

        var (result, bufferbloat) = await MeasuredRunAsync(provider, serverId, libreUrl, settings.Provider.Interface, cancellationToken);
        if (!result.Success)
        {
            LogRetrying(result.Error);
            serverId = serverOverride ?? await _serverSelector.SelectAsync(settings.Provider, cancellationToken);
            (result, bufferbloat) = await MeasuredRunAsync(provider, serverId, libreUrl, settings.Provider.Interface, cancellationToken);
        }

        var previous = result.Success ? await _results.GetLatestCompletedAsync(cancellationToken) : null;
        var test = Record(result, type, settings.Targets, check.PublicIp, bufferbloat);
        test.Id = await _results.CreateAsync(test, cancellationToken);

        if (result.Success)
        {
            var recommendation = await _recommendations.RecalculateAsync(cancellationToken);
            await _dispatcher.PublishAsync(new TestFinished(test), cancellationToken);
            if (test.Healthy == false)
                await _dispatcher.PublishAsync(new TestUnhealthy(test), cancellationToken);
            else if (test.Healthy == true && previous?.Healthy == false)
                await _dispatcher.PublishAsync(new TestHealthyAgain(test), cancellationToken);
            if (recommendation != null)
                await _dispatcher.PublishAsync(new RecommendationsUpdated(recommendation), cancellationToken);
        }
        else
        {
            await _dispatcher.PublishAsync(new TestFailed(test), cancellationToken);
        }

        _events.PublishTestFinished(SpeedtestDto.From(test));
        return result;
    }

    private async Task<(SpeedtestExecutionResult Result, BufferbloatReading? Bufferbloat)> MeasuredRunAsync(
        SpeedtestProvider provider, string? serverId, string? libreUrl, string? networkInterface, CancellationToken cancellationToken)
    {
        await using var measuring = await _bufferbloat.StartAsync(cancellationToken);
        var result = await _runner.RunTestAsync(provider, serverId, libreUrl, networkInterface, cancellationToken);
        return (result, result.Success ? await measuring.StopAsync() : null);
    }

    private static Speedtest Record(SpeedtestExecutionResult result, TestType type, TargetSettings targets, string? publicIp, BufferbloatReading? bufferbloat) => new()
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
        Status = result.Success ? TestStatus.Completed : TestStatus.Failed,
        Healthy = result.Success ? targets.Evaluate(result.Ping, result.Download, result.Upload) : null,
        ThresholdPing = targets.Ping,
        ThresholdDownload = targets.Download,
        ThresholdUpload = targets.Upload,
        PacketLoss = result.Success ? result.PacketLoss : null,
        Bufferbloat = bufferbloat?.Milliseconds,
        LatencyIdle = bufferbloat?.IdleMilliseconds,
        LatencyLoaded = bufferbloat?.LoadedMilliseconds,
        LatencyLoadedTail = bufferbloat?.LoadedTailMilliseconds,
        DownloadBytes = result.DownloadBytes,
        UploadBytes = result.UploadBytes,
        PublicIp = publicIp,
        Created = DateTime.UtcNow
    };

    private async Task RecordSkippedAsync(TestType type, string reason, string? publicIp, CancellationToken cancellationToken)
    {
        var skipped = new Speedtest
        {
            Ping = -1,
            Download = -1,
            Upload = -1,
            Status = TestStatus.Skipped,
            Error = reason,
            Type = type,
            PublicIp = publicIp,
            Created = DateTime.UtcNow
        };

        skipped.Id = await _results.CreateAsync(skipped, cancellationToken);
        LogSkipped(reason);

        await _dispatcher.PublishAsync(new TestSkipped(skipped), cancellationToken);
        _events.PublishTestFinished(SpeedtestDto.From(skipped));
    }

    private static SpeedtestExecutionResult Failure(string error) => new() { Success = false, Error = error };

    [LoggerMessage(Level = LogLevel.Information, Message = "Speedtest skipped because tests are paused")]
    private partial void LogSkippedWhilePaused();

    [LoggerMessage(Level = LogLevel.Error, Message = "The manual speedtest could not start")]
    private partial void LogManualRunFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error running speedtest")]
    private partial void LogRunCrashed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Speedtest failed ({Error}). Retrying once...")]
    private partial void LogRetrying(string? error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Speedtest skipped: {Reason}")]
    private partial void LogSkipped(string reason);
}

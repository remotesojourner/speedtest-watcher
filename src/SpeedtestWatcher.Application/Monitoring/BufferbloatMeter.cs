using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Application.Monitoring;

public sealed partial class BufferbloatMeter
{
    private readonly IConnectionProbe _probe;
    private readonly ISettingsStore _settings;
    private readonly TimeProvider _time;
    private readonly BufferbloatSampling _sampling;
    private readonly ILogger<BufferbloatMeter> _logger;

    public BufferbloatMeter(IConnectionProbe probe, ISettingsStore settings, TimeProvider time, BufferbloatSampling sampling, ILogger<BufferbloatMeter> logger)
    {
        _probe = probe;
        _settings = settings;
        _time = time;
        _sampling = sampling;
        _logger = logger;
    }

    public async Task<BufferbloatMeasurement> StartAsync(CancellationToken cancellationToken = default)
    {
        var targets = (await _settings.GetAsync(cancellationToken)).Monitoring.Targets;
        var measurement = new BufferbloatMeasurement(_probe, targets, _sampling, _time, LogSamplingFailed);
        await measurement.StartAsync(cancellationToken);
        return measurement;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sampling latency around a speedtest failed, so it has no bufferbloat figure")]
    private partial void LogSamplingFailed(Exception exception);
}

public sealed class BufferbloatMeasurement : IAsyncDisposable
{
    private readonly IConnectionProbe _probe;
    private readonly IReadOnlyList<ProbeTarget> _targets;
    private readonly BufferbloatSampling _sampling;
    private readonly TimeProvider _time;
    private readonly Action<Exception> _onFailure;
    private readonly List<double> _idle = [];
    private readonly List<double> _loaded = [];
    private readonly CancellationTokenSource _stopped = new();

    private Task _sampler = Task.CompletedTask;
    private long _loadStarted;

    public BufferbloatMeasurement(
        IConnectionProbe probe,
        IReadOnlyList<ProbeTarget> targets,
        BufferbloatSampling sampling,
        TimeProvider time,
        Action<Exception> onFailure)
    {
        _probe = probe;
        _targets = targets;
        _sampling = sampling;
        _time = time;
        _onFailure = onFailure;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_targets.Count == 0) return;

        await SampleAsync(_idle, _sampling.IdleWindow, cancellationToken);
        _loadStarted = _time.GetTimestamp();
        _sampler = SampleAsync(_loaded, Timeout.InfiniteTimeSpan, _stopped.Token);
    }

    public async Task<BufferbloatReading?> StopAsync()
    {
        await _stopped.CancelAsync();
        await _sampler;
        return _loadStarted == 0 ? null : Bufferbloat.From(_idle, _loaded, _time.GetElapsedTime(_loadStarted), _sampling);
    }

    public async ValueTask DisposeAsync()
    {
        await _stopped.CancelAsync();
        await _sampler;
        _stopped.Dispose();
    }

    private async Task SampleAsync(List<double> samples, TimeSpan window, CancellationToken cancellationToken)
    {
        var started = _time.GetTimestamp();
        try
        {
            while (!cancellationToken.IsCancellationRequested && (window == Timeout.InfiniteTimeSpan || _time.GetElapsedTime(started) < window))
            {
                var round = _time.GetTimestamp();
                var result = await _probe.ProbeAsync(_targets, _sampling.Timeout, cancellationToken);
                if (result.FastestMilliseconds is { } fastest) samples.Add(fastest);

                var rest = _sampling.Cadence - _time.GetElapsedTime(round);
                if (rest > TimeSpan.Zero) await Task.Delay(rest, _time, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _onFailure(ex);
        }
    }
}

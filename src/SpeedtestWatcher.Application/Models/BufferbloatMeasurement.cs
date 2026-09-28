using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Utils;

namespace SpeedtestWatcher.Application.Models;

public sealed class BufferbloatMeasurement : IAsyncDisposable
{
    private readonly IConnectionProbeService _probe;
    private readonly INetworkTrafficService _traffic;
    private readonly IReadOnlyList<ProbeTarget> _targets;
    private readonly BufferbloatSampling _sampling;
    private readonly TimeProvider _time;
    private readonly Action<Exception> _onFailure;
    private readonly List<double> _idle = [];
    private readonly List<LatencySample> _loaded = [];
    private readonly CancellationTokenSource _stopped = new();

    private Task _sampler = Task.CompletedTask;
    private long _loadStarted;

    public BufferbloatMeasurement(
        IConnectionProbeService probe,
        INetworkTrafficService traffic,
        IReadOnlyList<ProbeTarget> targets,
        BufferbloatSampling sampling,
        TimeProvider time,
        Action<Exception> onFailure)
    {
        _probe = probe;
        _traffic = traffic;
        _targets = targets;
        _sampling = sampling;
        _time = time;
        _onFailure = onFailure;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_targets.Count == 0) return;

        await SampleIdleAsync(cancellationToken);
        _loadStarted = _time.GetTimestamp();
        _sampler = SampleUnderLoadAsync(_stopped.Token);
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

    private async Task SampleIdleAsync(CancellationToken cancellationToken)
    {
        var started = _time.GetTimestamp();
        await SampleAsync(() => _time.GetElapsedTime(started) < _sampling.IdleWindow, _idle.Add, cancellationToken);
    }

    private async Task SampleUnderLoadAsync(CancellationToken cancellationToken)
    {
        var before = _traffic.Read();
        await SampleAsync(() => true, milliseconds =>
        {
            var now = _traffic.Read();
            _loaded.Add(new LatencySample(milliseconds, Bufferbloat.DirectionOf(now.Since(before), _sampling.BytesThatMeanTransfer)));
            before = now;
        }, cancellationToken);
    }

    private async Task SampleAsync(Func<bool> keepGoing, Action<double> record, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && keepGoing())
            {
                var round = _time.GetTimestamp();
                var result = await _probe.ProbeAsync(_targets, _sampling.Timeout, cancellationToken);
                if (result.FastestMilliseconds is { } fastest) record(fastest);

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

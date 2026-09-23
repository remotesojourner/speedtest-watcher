using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.TestSupport;

public sealed class OfflineProbe : IConnectionProbe
{
    public OfflineProbe(double? milliseconds = 12.5)
    {
        Milliseconds = milliseconds;
    }

    public double? Milliseconds { get; set; }

    public Task<ProbeResult> ProbeAsync(IReadOnlyList<ProbeTarget> targets, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProbeResult(Milliseconds is null ? 0 : targets.Count, targets.Count, Milliseconds));
}

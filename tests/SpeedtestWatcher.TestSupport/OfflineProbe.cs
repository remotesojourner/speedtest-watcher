using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.TestSupport;

public sealed class OfflineProbe : IConnectionProbeService
{
    public OfflineProbe(double? milliseconds = 12.5)
    {
        Milliseconds = milliseconds;
    }

    public double? Milliseconds { get; set; }

    public Task<ProbeResult> ProbeAsync(IReadOnlyList<ProbeTarget> targets, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProbeResult(Milliseconds is null ? 0 : targets.Count, targets.Count, Milliseconds));
}

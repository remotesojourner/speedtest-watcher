using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IConnectionProbeService
{
    Task<ProbeResult> ProbeAsync(IReadOnlyList<ProbeTarget> targets, TimeSpan timeout, CancellationToken cancellationToken = default);
}

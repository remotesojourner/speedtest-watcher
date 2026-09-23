namespace SpeedtestWatcher.Application.Monitoring;

public sealed record ProbeResult(int Answered, int Asked, double? FastestMilliseconds)
{
    public bool Passed => Asked > 0 && Answered * 2 > Asked;
}

public interface IConnectionProbe
{
    Task<ProbeResult> ProbeAsync(IReadOnlyList<ProbeTarget> targets, TimeSpan timeout, CancellationToken cancellationToken = default);
}

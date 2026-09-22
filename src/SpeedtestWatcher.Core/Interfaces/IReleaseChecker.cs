namespace SpeedtestWatcher.Core.Interfaces;

public interface IReleaseChecker
{
    Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default);
}

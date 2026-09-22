namespace SpeedtestWatcher.Application.Updates;

public interface IReleaseChecker
{
    Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default);
}

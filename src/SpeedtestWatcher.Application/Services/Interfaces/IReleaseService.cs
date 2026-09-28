namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IReleaseService
{
    Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default);
}

namespace SpeedtestWatcher.Core.Interfaces;

public interface IOidcDiscovery
{
    Task<string?> FindProblemAsync(string authority, CancellationToken cancellationToken = default);
}

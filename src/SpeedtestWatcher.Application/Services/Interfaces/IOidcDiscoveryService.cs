namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IOidcDiscoveryService
{
    Task<string?> FindProblemAsync(string authority, CancellationToken cancellationToken = default);
}

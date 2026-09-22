namespace SpeedtestWatcher.Application.SignIn;

public interface IOidcDiscovery
{
    Task<string?> FindProblemAsync(string authority, CancellationToken cancellationToken = default);
}

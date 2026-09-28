namespace SpeedtestWatcher.Application.Providers;

public interface ICliProcessRunner
{
    Task<ProcessOutcome> RunAsync(
        string toolTitle, string binaryPath, IReadOnlyList<string> arguments, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}

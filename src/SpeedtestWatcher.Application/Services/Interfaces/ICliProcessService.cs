using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface ICliProcessService
{
    Task<ProcessOutcome> RunAsync(
        string toolTitle, string binaryPath, IReadOnlyList<string> arguments, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}

using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.Interfaces;

public interface ISpeedtestRunner
{
    Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default);
}

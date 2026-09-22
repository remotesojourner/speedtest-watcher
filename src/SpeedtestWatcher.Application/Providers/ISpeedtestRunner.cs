namespace SpeedtestWatcher.Application.Providers;

public interface ISpeedtestRunner
{
    Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default);
}

using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.IntegrationTests.Browser;

public sealed class HeldSpeedtestRunner : ISpeedtestRunner
{
    public const string DownloadShown = "987.6";

    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Release() => _released.TrySetResult();

    public async Task<SpeedtestExecutionResult> RunTestAsync(
        SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default)
    {
        await _released.Task.WaitAsync(cancellationToken);
        return new SpeedtestExecutionResult
        {
            Success = true,
            Ping = 9,
            Jitter = 0.3,
            Download = 987.6,
            Upload = 123.4,
            Time = 12,
            ServerId = 101,
            ServerName = "Test Fibre",
            ServerHost = "london.test.example"
        };
    }
}

namespace SpeedtestWatcher.Web.Background;

public static class BackgroundDelay
{
    /// <summary>
    /// Waits for <paramref name="delay"/>, returning false instead of throwing once the host is stopping.
    /// A cancellation that escapes ExecuteAsync is ignored by the host but breaks the debugger as user-unhandled.
    /// </summary>
    public static async Task<bool> WaitAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}

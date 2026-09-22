namespace SpeedtestWatcher.Application.Common;

internal static class BackgroundDelay
{
    public static readonly TimeSpan LongestStep = TimeSpan.FromDays(1);

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

    public static async Task<bool> WaitUntilAsync(DateTime dueUtc, TimeProvider time, CancellationToken stoppingToken, CancellationToken interrupted)
    {
        for (var remaining = dueUtc - time.GetUtcNow().UtcDateTime; remaining > TimeSpan.Zero; remaining = dueUtc - time.GetUtcNow().UtcDateTime)
        {
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, interrupted);
            try
            {
                await Task.Delay(remaining < LongestStep ? remaining : LongestStep, time, wait.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                return false;
            }
        }

        return true;
    }
}

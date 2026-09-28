using Microsoft.Extensions.Time.Testing;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal sealed class WaitAwareTimeProvider : FakeTimeProvider
{
    private readonly Lock _gate = new();
    private readonly Queue<TimeSpan> _startedWaits = new();
    private readonly Queue<TaskCompletionSource<TimeSpan>> _waitingForWait = new();

    public WaitAwareTimeProvider(DateTimeOffset start) : base(start)
    {
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        if (dueTime <= TimeSpan.Zero || dueTime == Timeout.InfiniteTimeSpan) return timer;

        lock (_gate)
        {
            if (_waitingForWait.TryDequeue(out var waiting)) waiting.SetResult(dueTime);
            else _startedWaits.Enqueue(dueTime);
        }

        return timer;
    }

    public Task<TimeSpan> NextWaitAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_startedWaits.TryDequeue(out var wait)) return Task.FromResult(wait);

            var waiting = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingForWait.Enqueue(waiting);
            return waiting.Task.WaitAsync(cancellationToken);
        }
    }

    public async Task SkipNextWaitAsync(CancellationToken cancellationToken) => Advance(await NextWaitAsync(cancellationToken));

    public async Task<bool> SkipWaitsUntilAsync(Task done, TimeSpan window, CancellationToken cancellationToken)
    {
        var until = GetUtcNow() + window;
        while (true)
        {
            var wait = NextWaitAsync(cancellationToken);
            if (await Task.WhenAny(done, wait) == done) return true;

            var due = await wait;
            if (GetUtcNow() + due > until) return false;
            Advance(due);
        }
    }
}

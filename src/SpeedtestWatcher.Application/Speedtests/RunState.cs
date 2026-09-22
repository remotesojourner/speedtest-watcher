using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Application.Speedtests;

public sealed class RunState : IDisposable
{
    private readonly object _lock = new();
    private readonly IAppEvents _events;
    private bool _running;
    private DateTime? _resumesAt;
    private Timer? _timer;

    public RunState(IAppEvents events)
    {
        _events = events;
    }

    public bool IsRunning
    {
        get
        {
            lock (_lock) return _running;
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_lock)
            {
                if (_resumesAt == null) return false;
                if (_resumesAt == DateTime.MaxValue) return true;
                return DateTime.UtcNow < _resumesAt.Value;
            }
        }
    }

    public DateTime? ResumesAt
    {
        get
        {
            lock (_lock) return _resumesAt;
        }
    }

    public bool TryStartRun()
    {
        lock (_lock)
        {
            if (_running) return false;
            _running = true;
        }

        Publish();
        return true;
    }

    public void FinishRun()
    {
        lock (_lock)
        {
            _running = false;
        }

        Publish();
    }

    public void Pause(double? hours)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hours ?? 0, PauseRequest.MaxResumeInHours, nameof(hours));

        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;

            if (hours == null || hours < 0)
            {
                _resumesAt = DateTime.MaxValue;
            }
            else
            {
                _resumesAt = DateTime.UtcNow.AddHours(hours.Value);
                _timer = new Timer(_ => Resume(), null, TimeSpan.FromHours(hours.Value), Timeout.InfiniteTimeSpan);
            }
        }

        Publish();
    }

    public void Resume()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
            _resumesAt = null;
        }

        Publish();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void Publish() => _events.PublishRunStatusChanged(new RunStatus(IsRunning, IsPaused));
}

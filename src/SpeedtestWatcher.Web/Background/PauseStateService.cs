using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Background;

public sealed class PauseStateService : IPauseStateService, IDisposable
{
    private readonly object _lock = new();
    private bool _isRunning;
    private DateTime? _resumesAt;
    private Timer? _timer;

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

    public bool IsRunning
    {
        get
        {
            lock (_lock) return _isRunning;
        }
    }

    public DateTime? ResumesAt
    {
        get
        {
            lock (_lock) return _resumesAt;
        }
    }

    public event Action? OnStatusChanged;

    public void SetRunning(bool running)
    {
        lock (_lock)
        {
            _isRunning = running;
        }
        OnStatusChanged?.Invoke();
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
                var dueTime = TimeSpan.FromHours(hours.Value);
                _timer = new Timer(_ => Resume(), null, dueTime, Timeout.InfiniteTimeSpan);
            }
        }
        OnStatusChanged?.Invoke();
    }

    public void Resume()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
            _resumesAt = null;
        }
        OnStatusChanged?.Invoke();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}

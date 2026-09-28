using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed class ConnectionStateService
{
    private readonly IAppEventService _events;
    private readonly object _lock = new();
    private ConnectionSnapshot _snapshot = new(ConnectionHealth.Unknown, null, null, null);

    public ConnectionStateService(IAppEventService events)
    {
        _events = events;
    }

    public ConnectionSnapshot Current
    {
        get
        {
            lock (_lock) return _snapshot;
        }
    }

    public void Update(ConnectionHealth health, DateTime at, double? fastestMilliseconds)
    {
        lock (_lock)
        {
            var since = _snapshot.Health == health ? _snapshot.Since ?? at : at;
            _snapshot = new ConnectionSnapshot(health, since, at, fastestMilliseconds);
        }

        _events.PublishConnectionChanged(Current);
    }

    public void StopWatching()
    {
        lock (_lock)
        {
            _snapshot = new ConnectionSnapshot(ConnectionHealth.Unknown, null, null, null);
        }

        _events.PublishConnectionChanged(Current);
    }
}

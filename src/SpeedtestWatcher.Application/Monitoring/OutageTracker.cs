using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Application.Monitoring;

public enum ConnectionChange
{
    None,
    WentDown,
    CameUp
}

public sealed class OutageTracker
{
    private int _failedInARow;
    private int _passedInARow;

    public ConnectionHealth Health { get; private set; } = ConnectionHealth.Unknown;

    public ConnectionChange Record(bool passed, MonitoringSettings settings)
    {
        _failedInARow = passed ? 0 : _failedInARow + 1;
        _passedInARow = passed ? _passedInARow + 1 : 0;

        if (Health != ConnectionHealth.Down && _failedInARow >= settings.RoundsToGoDown)
        {
            Health = ConnectionHealth.Down;
            return ConnectionChange.WentDown;
        }

        if (Health == ConnectionHealth.Up || _passedInARow < settings.RoundsToGoUp) return ConnectionChange.None;

        var wasDown = Health == ConnectionHealth.Down;
        Health = ConnectionHealth.Up;
        return wasDown ? ConnectionChange.CameUp : ConnectionChange.None;
    }

    public void Forget()
    {
        _failedInARow = 0;
        _passedInARow = 0;
        Health = ConnectionHealth.Unknown;
    }
}

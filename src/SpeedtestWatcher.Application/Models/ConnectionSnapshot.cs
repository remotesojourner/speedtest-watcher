using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Models;

public sealed record ConnectionSnapshot(ConnectionHealth Health, DateTime? Since, DateTime? LastRoundAt, double? FastestMilliseconds)
{
    public bool IsDown => Health == ConnectionHealth.Down;
}

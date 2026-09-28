namespace SpeedtestWatcher.Application.Enums;

public enum MessageKind
{
    Finished,
    Failed,
    Unhealthy,
    HealthyAgain,
    Skipped,
    ConnectionLost,
    ConnectionRestored
}

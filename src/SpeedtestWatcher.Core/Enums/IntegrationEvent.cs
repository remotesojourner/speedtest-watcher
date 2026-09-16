namespace SpeedtestWatcher.Core.Enums;

public enum IntegrationEvent
{
    TestStarted,
    TestFinished,
    TestFailed,
    TestUnhealthy,
    TestSkipped,
    MinutePassed,
    RecommendationsUpdated,
    ConfigUpdated
}

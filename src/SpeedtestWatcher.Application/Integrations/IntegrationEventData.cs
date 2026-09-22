namespace SpeedtestWatcher.Application.Integrations;

internal static class IntegrationEventData
{
    public static object? For(IntegrationEvent integrationEvent) => integrationEvent switch
    {
        TestStarted started => new { provider = started.Provider, type = started.Type },
        TestFinished finished => finished.Result,
        TestUnhealthy unhealthy => unhealthy.Result,
        TestFailed failed => failed.Result,
        TestSkipped skipped => skipped.Result,
        RecommendationsUpdated updated => updated.Recommendation,
        ConfigUpdated updated => new { key = updated.Key, value = updated.Value },
        _ => null
    };
}

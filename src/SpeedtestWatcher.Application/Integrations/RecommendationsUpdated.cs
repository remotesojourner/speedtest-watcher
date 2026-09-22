using SpeedtestWatcher.Application.Recommendations;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record RecommendationsUpdated(Recommendation Recommendation) : IntegrationEvent;

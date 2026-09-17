using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Events;

public abstract record IntegrationEvent;

public sealed record TestStarted(string Provider, string Type) : IntegrationEvent;

public sealed record TestFinished(Speedtest Result) : IntegrationEvent;

public sealed record TestUnhealthy(Speedtest Result) : IntegrationEvent;

public sealed record TestFailed(Speedtest Result) : IntegrationEvent;

public sealed record TestSkipped(Speedtest Result) : IntegrationEvent;

public sealed record RecommendationsUpdated(Recommendation Recommendation) : IntegrationEvent;

public sealed record ConfigUpdated(string Key, string Value) : IntegrationEvent;

public sealed record Heartbeat : IntegrationEvent;

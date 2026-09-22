using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record TestUnhealthy(Speedtest Result) : IntegrationEvent;

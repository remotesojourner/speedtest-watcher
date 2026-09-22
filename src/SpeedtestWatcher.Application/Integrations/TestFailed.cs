using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record TestFailed(Speedtest Result) : IntegrationEvent;

using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record TestSkipped(Speedtest Result) : IntegrationEvent;

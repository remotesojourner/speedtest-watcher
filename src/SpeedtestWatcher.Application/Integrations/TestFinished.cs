using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record TestFinished(Speedtest Result) : IntegrationEvent;

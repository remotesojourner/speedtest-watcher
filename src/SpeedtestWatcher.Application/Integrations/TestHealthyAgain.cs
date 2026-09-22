using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record TestHealthyAgain(Speedtest Result) : IntegrationEvent;

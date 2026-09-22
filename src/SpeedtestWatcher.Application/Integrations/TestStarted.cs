using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record TestStarted(SpeedtestProvider Provider, TestType Type) : IntegrationEvent;

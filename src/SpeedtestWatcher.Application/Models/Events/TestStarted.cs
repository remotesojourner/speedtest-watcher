using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Models.Events;

public sealed record TestStarted(SpeedtestProvider Provider, TestType Type) : IntegrationEvent;

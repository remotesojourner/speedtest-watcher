using SpeedtestWatcher.Application.Models.Entities;

namespace SpeedtestWatcher.Application.Models.Events;

public sealed record TestUnhealthy(Speedtest Result) : IntegrationEvent;

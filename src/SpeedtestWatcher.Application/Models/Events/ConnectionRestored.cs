namespace SpeedtestWatcher.Application.Models.Events;

public sealed record ConnectionRestored(DateTime At, TimeSpan Downtime) : IntegrationEvent;

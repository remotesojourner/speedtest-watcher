namespace SpeedtestWatcher.Application.Integrations;

public sealed record ConnectionRestored(DateTime At, TimeSpan Downtime) : IntegrationEvent;

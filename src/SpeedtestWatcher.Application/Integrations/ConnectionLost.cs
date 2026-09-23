namespace SpeedtestWatcher.Application.Integrations;

public sealed record ConnectionLost(DateTime Since) : IntegrationEvent;

namespace SpeedtestWatcher.Application.Integrations;

public sealed record ConfigUpdated(string Key, string Value) : IntegrationEvent;

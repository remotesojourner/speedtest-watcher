namespace SpeedtestWatcher.Application.Models.Events;

public sealed record ConfigUpdated(string Key, string Value) : IntegrationEvent;

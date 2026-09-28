namespace SpeedtestWatcher.Application.Models.Events;

public sealed record ConnectionLost(DateTime Since) : IntegrationEvent;

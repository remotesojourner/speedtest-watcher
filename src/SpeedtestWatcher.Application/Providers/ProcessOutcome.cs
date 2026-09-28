namespace SpeedtestWatcher.Application.Providers;

public sealed record ProcessOutcome(ToolOutput? Output, string? FailureMessage);

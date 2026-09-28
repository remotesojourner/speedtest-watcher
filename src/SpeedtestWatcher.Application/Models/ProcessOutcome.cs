namespace SpeedtestWatcher.Application.Models;

public sealed record ProcessOutcome(ToolOutput? Output, string? FailureMessage);

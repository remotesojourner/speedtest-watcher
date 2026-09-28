using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Models;

public sealed record IntegrationResult(IntegrationOutcome Outcome, string? Error = null)
{
    public static IntegrationResult Sent { get; } = new(IntegrationOutcome.Sent);

    public static IntegrationResult NotApplicable { get; } = new(IntegrationOutcome.NotApplicable);

    public static IntegrationResult Failed(string error) => new(IntegrationOutcome.Failed, error);
}
